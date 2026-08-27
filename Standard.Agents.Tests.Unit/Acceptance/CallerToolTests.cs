// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Sessions;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Brokers.Generators;
using Standard.Agents.Models.Brokers.Generators.V1;
using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Foundations.Skills;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// Caller tools (docs/per-request-inference.md §6). In the OpenAI protocol the server never
// executes a caller's tool: the model names one, the server returns the call, and the CALLER
// executes it and posts the result back. Caller tools are therefore vocabulary handed to the
// model, never capability granted to the agent — and a call naming one is a terminal answer
// addressed to the caller, riding the same pending-effect seam a held approval rides.
public class CallerToolTests : IDisposable
{
    private readonly string sessionsPath =
        Path.Combine(Path.GetTempPath(), $"standard-agent-caller-tools-{Guid.NewGuid():n}");

    public void Dispose()
    {
        if (Directory.Exists(this.sessionsPath))
        {
            Directory.Delete(this.sessionsPath, recursive: true);
        }
    }

    private static StandardAgent AgentWith(Func<string, string, ValueTask<string>> brain)
    {
        var skillBroker = new Mock<ISkillBroker>();

        skillBroker.Setup(broker => broker.SelectSkillsAsync())
            .ReturnsAsync(new List<Skill> { new() { Content = "You are a helpful agent." } });

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .OnBrain(brain);
    }

    // The foreign call is a pending effect: the run pauses, something outside this process must
    // act and report back — built for human approval, structurally identical here. The authority
    // over this one is the caller.
    [Fact]
    public async Task ShouldTreatACallNamingACallerToolAsATerminalAnswerAsync()
    {
        // given — a brain that wants the caller's tool, which the agent cannot and must not run
        StandardAgent agent = AgentWith((systemPrompt, userPrompt) =>
            ValueTask.FromResult("""ACTION: send_email: {"to":"jane@acme.com"}"""))
            .Sessions(this.sessionsPath)
            .MaxTurns(2);

        var request = new PromptRequest
        {
            Prompt = "email jane the report",
            SessionId = "caller-run-1",

            CallerTools =
            [
                new ToolDefinition(
                    Name: "send_email",
                    Description: "Sends an email from the caller's own outbox.",
                    ParametersJson: "{}")
            ]
        };

        // when
        AgentOutcome outcome = await agent.RunAsync(request);

        // then — the run paused rather than recovered, refused, or executed anything
        outcome.Status.Should().Be(AgentStatus.AwaitingInput);
        outcome.Result.Should().Contain("send_email");

        // and the call itself rode out on the session, so a different process can hand the
        // caller the act rather than only the news that something is waiting
        AgentSession? session =
            await new FileSessionBroker(this.sessionsPath).SelectSessionAsync("caller-run-1");

        session.Should().NotBeNull();
        session!.PendingEffect.Should().NotBeNull();
        session.PendingEffect!.ToolName.Should().Be("send_email");
        session.PendingEffect.Arguments.Should().Contain("jane@acme.com");
    }

    private sealed class RecordingNativeBroker : IGeneratorBrokerV1
    {
        public IReadOnlyList<ToolDefinition> CapturedTools { get; private set; } = [];

        public ValueTask<GenerationResult> GenerateAsync(
            IReadOnlyList<ConversationMessage> messages,
            IReadOnlyList<ToolDefinition> tools)
        {
            this.CapturedTools = tools;

            return ValueTask.FromResult(new GenerationResult { Content = "ok" });
        }
    }

    private sealed class CalculatorTool : ITool
    {
        public string Name => "calculator";
        public string Description => "Evaluates arithmetic.";
        public string Parameters => "{}";

        public ValueTask<string> ExecuteAsync(string input) =>
            ValueTask.FromResult("4183");
    }

    // One vocabulary, two owners: the model sees the configured tools and the caller's tools
    // side by side, and only Direction knows which words are whose (design §6.1).
    [Fact]
    public async Task ShouldAdvertiseCallerToolsBesideConfiguredOnesNativelyAsync()
    {
        // given
        var broker = new RecordingNativeBroker();

        var skillBroker = new Mock<ISkillBroker>();

        skillBroker.Setup(b => b.SelectSkillsAsync())
            .ReturnsAsync(new List<Skill> { new() { Content = "You are a helpful agent." } });

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(b => b.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(b => b.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        StandardAgent agent = new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .Tool(new CalculatorTool())
            .UseNativeBrain(broker);

        var request = new PromptRequest
        {
            Prompt = "hello",

            CallerTools =
            [
                new ToolDefinition(
                    Name: "send_email",
                    Description: "Sends an email from the caller's own outbox.",
                    ParametersJson: "{}")
            ]
        };

        // when
        await agent.ProcessPromptAsync(request);

        // then
        broker.CapturedTools.Should().Contain(tool => tool.Name == "calculator");
        broker.CapturedTools.Should().Contain(tool => tool.Name == "send_email");
    }
}
