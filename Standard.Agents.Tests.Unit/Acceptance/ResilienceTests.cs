// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Brokers.Generators.V1;
using Standard.Agents.Models.Foundations.Skills;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// Cancellation, retry and budget end to end (SPEC.md §4.10). An agent that cannot be stopped,
// cannot survive a transient failure, and cannot say what it spent is not something an
// enterprise can operate, however correct each decision is.
public class ResilienceTests
{
    private static StandardAgent AgentThatLoops(Func<int, string> replyForTurn)
    {
        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        int turn = 0;

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .OnBrain(async (systemPrompt, userPrompt) => replyForTurn(turn++));
    }

    private static StandardAgent AgentWithNativeBrainThatAlwaysFails()
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
            .OnNativeBrain((messages, tools) =>
                throw new HttpRequestException(
                    "the native provider is down",
                    inner: null,
                    statusCode: System.Net.HttpStatusCode.ServiceUnavailable));
    }

    // Found in the 2026-09-04 principal review (F-07): the fallback broker cast the configured
    // string answer to whatever T the caller asked for. On the text protocol T is string and the
    // cast is free; on the native protocol T is GenerationResult, so the moment the circuit
    // opened — exactly when the control is supposed to work — the fallback threw
    // InvalidCastException instead of degrading. A control that holds on one protocol and not
    // the other is a control a caller can step around by changing configuration.
    [Fact]
    public async Task ShouldFallBackOnTheNativeProtocolWhenTheCircuitOpensAsync()
    {
        // given
        StandardAgent agent = AgentWithNativeBrainThatAlwaysFails()
            .Fallback(
                fallback: async () => "Answering from the standby model.",
                failuresBeforeOpen: 1);

        // when
        string actualAnswer = await agent.ProcessPromptAsync("what is the answer");

        // then
        actualAnswer.Should().Be("Answering from the standby model.");
    }

    [Fact]
    public async Task ShouldStopTheLoopWhenCancelledAsync()
    {
        // given — a Brain that would loop forever through tool calls
        using var cancellation = new CancellationTokenSource();
        int brainCalls = 0;

        StandardAgent agent = AgentThatLoops(turn =>
        {
            brainCalls++;
            cancellation.Cancel();

            return "ACTION: nonexistent_tool: keep going";
        });

        // when
        string actualResult =
            await agent.ProcessPromptAsync(prompt: "loop forever", cancellation.Token);

        // then — stopped at the next turn boundary, and said so rather than answering
        actualResult.Should().Contain("cancelled");
        brainCalls.Should().Be(1);
    }

    [Fact]
    public async Task ShouldNotReportCancellationAsAnAnswerAsync()
    {
        // given
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        StandardAgent agent = AgentThatLoops(turn => "FINAL: 42");

        // when
        string actualResult =
            await agent.ProcessPromptAsync(prompt: "what is the answer?", cancellation.Token);

        // then — a cancelled run's result is not an answer
        actualResult.Should().NotBe("42");
        actualResult.Should().Contain("cancelled");
    }

    [Fact]
    public async Task ShouldStopTheLoopWhenTheTimeBudgetIsExhaustedAsync()
    {
        // given — every turn asks for a tool that does not exist, so the loop keeps going
        StandardAgent agent =
            AgentThatLoops(turn => "ACTION: nonexistent_tool: keep going")
                .MaxTurns(50)
                .Budget(maxWallClock: TimeSpan.Zero);

        // when
        string actualResult = await agent.ProcessPromptAsync(prompt: "loop forever");

        // then — exhaustion is distinguishable from a refusal
        actualResult.Should().Contain("time budget");
        actualResult.Should().NotContain("not able to help");
    }

    [Fact]
    public async Task ShouldRunUnboundedWhenNoBudgetIsConfiguredAsync()
    {
        // given
        StandardAgent agent = AgentThatLoops(turn => "FINAL: 42");

        // when
        string actualResult = await agent.ProcessPromptAsync(prompt: "what is the answer?");

        // then — absent a budget the agent behaves exactly as it did
        actualResult.Should().Be("42");
    }
}
