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

    private sealed class CountingCalculatorTool : ITool
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

    // Name collision resolved by the perimeter rule (design §6.1): configured wins, and
    // unambiguously — a caller declaring a tool named like the deployment's own cannot shadow
    // it. The call means the configured tool, executed locally, under every configured control.
    [Fact]
    public async Task ShouldLetTheConfiguredToolKeepItsNameAsync()
    {
        // given — the caller claims "calculator" for itself
        var tool = new CountingCalculatorTool();
        int call = 0;

        StandardAgent agent = AgentWith((systemPrompt, userPrompt) =>
            ValueTask.FromResult(call++ == 0
                ? "ACTION: calculator: 47*89"
                : "FINAL: 4183"))
            .Tool(tool);

        var request = new PromptRequest
        {
            Prompt = "what is 47 times 89?",

            CallerTools =
            [
                new ToolDefinition(
                    Name: "calculator",
                    Description: "The caller's own calculator.",
                    ParametersJson: "{}")
            ]
        };

        // when
        AgentOutcome outcome = await agent.RunAsync(request);

        // then — executed here, not returned to the caller
        tool.ExecutionCount.Should().Be(1);
        outcome.Status.Should().Be(AgentStatus.Responded);
        outcome.Result.Should().Be("4183");
    }

    private sealed class YieldingNativeBroker : IGeneratorBrokerV1
    {
        public ValueTask<GenerationResult> GenerateAsync(
            IReadOnlyList<ConversationMessage> messages,
            IReadOnlyList<ToolDefinition> tools) =>
            ValueTask.FromResult(new GenerationResult
            {
                ToolCalls =
                [
                    new ModelToolCall(
                        Id: "call_9",
                        Name: "send_email",
                        ArgumentsJson: """{"to":"jane@acme.com"}""")
                ]
            });
    }

    // A stateless deployment has no session, and the client owns the transcript — the exposed
    // protocol this seam exists for (docs/per-request-inference.md §6.2). The pending call must
    // therefore reach the caller on the OUTCOME itself, carrying the id the model minted, or the
    // exposer cannot render the yield at all.
    [Fact]
    public async Task ShouldHandTheCallerTheirCallOnTheOutcomeWithoutASessionAsync()
    {
        // given — no sessions configured, a native brain that wants the caller's tool
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
            .UseNativeBrain(new YieldingNativeBroker());

        var request = new PromptRequest
        {
            Prompt = "email jane the report",

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

        // then — the call itself, on the outcome, with the model's own id
        outcome.Status.Should().Be(AgentStatus.AwaitingInput);
        outcome.PendingEffect.Should().NotBeNull();
        outcome.PendingEffect!.ToolName.Should().Be("send_email");
        outcome.PendingEffect.CallId.Should().Be("call_9");
        outcome.PendingEffect.Arguments.Should().Contain("jane@acme.com");
    }

    private sealed class MessageRecordingNativeBroker : IGeneratorBrokerV1
    {
        public IReadOnlyList<ConversationMessage> CapturedMessages { get; private set; } = [];

        public ValueTask<GenerationResult> GenerateAsync(
            IReadOnlyList<ConversationMessage> messages,
            IReadOnlyList<ToolDefinition> tools)
        {
            this.CapturedMessages = messages;

            return ValueTask.FromResult(new GenerationResult { Content = "ok" });
        }
    }

    private static StandardAgent BareAgent()
    {
        var skillBroker = new Mock<ISkillBroker>();

        skillBroker.Setup(b => b.SelectSkillsAsync())
            .ReturnsAsync(new List<Skill> { new() { Content = "You are a helpful agent." } });

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(b => b.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(b => b.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object);
    }

    // The exposed protocols this seam exists for are stateless: the client re-posts the whole
    // conversation, prior tool exchanges included. A run that cannot receive that transcript
    // starts from nothing every time — and the second half of the yield (the caller answering a
    // call the run handed out) becomes impossible to express.
    [Fact]
    public async Task ShouldCarryTheCallerOwnedTranscriptToTheNativeBrainAsync()
    {
        // given
        var broker = new MessageRecordingNativeBroker();
        StandardAgent agent = BareAgent().UseNativeBrain(broker);

        var request = new PromptRequest
        {
            Prompt = "and then?",
            History = [new AgentTurn("What is 2+2?", "4")],

            ToolExchanges =
            [
                new ToolExchange(
                    CallId: "call_9",
                    ToolName: "send_email",
                    ArgumentsJson: """{"to":"jane@acme.com"}""",
                    Result: "sent")
            ]
        };

        // when
        await agent.ProcessPromptAsync(request);

        // then — the prior turn as user/assistant messages, and the exchange as a tool message
        // still naming the call the model minted
        broker.CapturedMessages.Should().Contain(message =>
            message.Role == MessageRole.User && message.Content == "What is 2+2?");

        broker.CapturedMessages.Should().Contain(message =>
            message.Role == MessageRole.Assistant && message.Content == "4");

        broker.CapturedMessages.Should().Contain(message =>
            message.Role == MessageRole.Tool
                && message.ToolCallId == "call_9"
                && message.Content == "sent");
    }

    // The same transcript on the text protocol: history renders into the conversation the
    // model is shown, exactly as a session's history would.
    [Fact]
    public async Task ShouldCarryTheCallerOwnedTranscriptOnTheTextProtocolAsync()
    {
        // given
        string capturedUserPrompt = string.Empty;

        StandardAgent agent = BareAgent().OnBrain((systemPrompt, userPrompt) =>
        {
            capturedUserPrompt = userPrompt;

            return ValueTask.FromResult("FINAL: ok");
        });

        var request = new PromptRequest
        {
            Prompt = "and then?",
            History = [new AgentTurn("What is 2+2?", "4")]
        };

        // when
        await agent.ProcessPromptAsync(request);

        // then
        capturedUserPrompt.Should().Contain("What is 2+2?");
        capturedUserPrompt.Should().Contain("4");
    }

    // When a session exists it wins: the deployment's record of the conversation beats the
    // caller's retelling of it — the same precedence every request field obeys (design §4).
    [Fact]
    public async Task ShouldLetTheSessionsHistoryWinOverTheRequestsAsync()
    {
        // given — a real session holding a real prior turn
        string capturedUserPrompt = string.Empty;
        int call = 0;

        StandardAgent agent = BareAgent()
            .Sessions(this.sessionsPath)
            .OnBrain((systemPrompt, userPrompt) =>
            {
                capturedUserPrompt = userPrompt;

                return ValueTask.FromResult(call++ == 0
                    ? "FINAL: Tokyo"
                    : "FINAL: ok");
            });

        await agent.ProcessPromptAsync("Capital of Japan?", "transcript-1", CancellationToken.None);

        var request = new PromptRequest
        {
            Prompt = "and then?",
            SessionId = "transcript-1",
            History = [new AgentTurn("a retelling that never happened", "made up")]
        };

        // when
        await agent.ProcessPromptAsync(request);

        // then — the session's turn is what the brain saw, not the caller's retelling
        capturedUserPrompt.Should().Contain("Capital of Japan?");
        capturedUserPrompt.Should().NotContain("a retelling that never happened");
    }
}
