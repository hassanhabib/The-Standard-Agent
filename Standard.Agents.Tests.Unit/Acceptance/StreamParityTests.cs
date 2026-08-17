// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Foundations.Skills;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// One prompt, two doors. Invariant 6 already says a control must hold "on every path by which a
// prompt can be processed", and names the batched and the streamed loop specifically. It says that
// about guardian neutralization, but the reason generalizes: a control a caller can step around by
// changing method is not a control.
//
// Budgets, cancellation and sessions were enforced on the batched path only.
public class StreamParityTests : IDisposable
{
    private readonly string sessionsPath =
        Path.Combine(Path.GetTempPath(), $"standard-agent-stream-{Guid.NewGuid():n}");

    public void Dispose()
    {
        if (Directory.Exists(this.sessionsPath))
        {
            Directory.Delete(this.sessionsPath, recursive: true);
        }
    }

    private sealed class CountingTool : ITool
    {
        public string Name => "calculator";
        public string Description => "Evaluates arithmetic.";
        public string Parameters => "{}";

        public int ExecutionCount { get; private set; }

        public ValueTask<string> ExecuteAsync(string input)
        {
            ExecutionCount++;

            return ValueTask.FromResult("4183");
        }
    }

    private static StandardAgent AgentThatLoops(
        CountingTool tool,
        Func<string, string, ValueTask<string>> brain)
    {
        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .Tool(tool)
            .OnBrain(brain);
    }

    private static async Task<List<AgentStreamEvent>> DrainAsync(
        IAsyncEnumerable<AgentStreamEvent> events)
    {
        List<AgentStreamEvent> drained = [];

        await foreach (AgentStreamEvent streamEvent in events)
        {
            drained.Add(streamEvent);
        }

        return drained;
    }

    [Fact]
    public async Task ShouldStopAStreamedRunWhenCancelledAsync()
    {
        // given — a model that would loop forever, and a token already cancelled
        var tool = new CountingTool();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        StandardAgent agent = AgentThatLoops(
            tool,
            async (systemPrompt, userPrompt) => "ACTION: calculator: 47*89")
            .MaxTurns(50);

        // when
        List<AgentStreamEvent> actualEvents =
            await DrainAsync(agent.StreamPromptAsync("loop", cancellation.Token));

        // then — a cancelled run stops at the turn boundary and says so, on this path too
        tool.ExecutionCount.Should().Be(0);

        actualEvents.Should().Contain(streamEvent =>
            streamEvent.Content.Contains("cancelled"));
    }

    [Fact]
    public async Task ShouldStopAStreamedRunWhenTheBudgetIsExhaustedAsync()
    {
        // given — a model that would loop forever, and no time to do it in
        var tool = new CountingTool();

        StandardAgent agent = AgentThatLoops(
            tool,
            async (systemPrompt, userPrompt) => "ACTION: calculator: 47*89")
            .MaxTurns(50)
            .Budget(maxWallClock: TimeSpan.Zero);

        // when
        List<AgentStreamEvent> actualEvents =
            await DrainAsync(agent.StreamPromptAsync("loop"));

        // then — a budget a caller can step around by streaming is not a budget
        actualEvents.Should().Contain(streamEvent =>
            streamEvent.Content.Contains("budget"));
    }

    [Fact]
    public async Task ShouldCarryTheConversationOnAStreamedRunAsync()
    {
        // given — the first prompt is answered on the batched path
        var tool = new CountingTool();
        string secondPrompt = string.Empty;

        StandardAgent first = AgentThatLoops(
            tool,
            async (systemPrompt, userPrompt) => "FINAL: Paris")
            .Sessions(this.sessionsPath);

        await first.ProcessPromptAsync(
            "what is the capital of France?", "trip-3", CancellationToken.None);

        // when — the follow-up is streamed
        StandardAgent second = AgentThatLoops(
            tool,
            async (systemPrompt, userPrompt) =>
            {
                secondPrompt = userPrompt;

                return "FINAL: about two million";
            })
            .Sessions(this.sessionsPath);

        await DrainAsync(second.StreamPromptAsync(
            "and how many people live there?", "trip-3", CancellationToken.None));

        // then — the streamed path loads the conversation like the batched one does
        secondPrompt.Should().Contain("Paris");
    }

    [Fact]
    public async Task ShouldRecordAStreamedExchangeInTheConversationAsync()
    {
        // given
        var tool = new CountingTool();

        StandardAgent agent = AgentThatLoops(
            tool,
            async (systemPrompt, userPrompt) => "FINAL: Paris")
            .Sessions(this.sessionsPath);

        // when — the first exchange happens on the streamed path
        await DrainAsync(agent.StreamPromptAsync(
            "what is the capital of France?", "trip-9", CancellationToken.None));

        string thirdPrompt = string.Empty;

        StandardAgent follower = AgentThatLoops(
            tool,
            async (systemPrompt, userPrompt) =>
            {
                thirdPrompt = userPrompt;

                return "FINAL: about two million";
            })
            .Sessions(this.sessionsPath);

        await follower.ProcessPromptAsync(
            "and how many people live there?", "trip-9", CancellationToken.None);

        // then — a streamed answer is part of the conversation, not lost from it
        thirdPrompt.Should().Contain("Paris");
    }
}
