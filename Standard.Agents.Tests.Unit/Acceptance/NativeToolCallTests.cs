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
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// Native tool calling (SPEC.md §6). The text protocol asks a model to imitate a format; a
// tool_call is the thing hosted models are actually tuned for. These pin that adopting it changes
// how a choice is READ, not what the agent is — the same tools, the same perimeter, the same
// answer at the end.
public class NativeToolCallTests
{
    private sealed class CalculatorTool : ITool
    {
        public string Name => "calculator";
        public string Description => "Evaluates arithmetic.";
        public string Parameters => """{"type":"object","properties":{"expression":{"type":"string"}}}""";

        public IReadOnlyList<string> ReceivedInputs => this.received;
        private readonly List<string> received = [];

        public ValueTask<string> ExecuteAsync(string input)
        {
            this.received.Add(input);

            return ValueTask.FromResult("4183");
        }
    }

    private static StandardAgent AgentWith(
        CalculatorTool tool,
        Func<IReadOnlyList<ToolDefinition>, int, GenerationResult> reply)
    {
        var skillBroker = new Mock<ISkillBroker>();

        skillBroker.Setup(broker => broker.SelectSkillsAsync())
            .ReturnsAsync(new List<Skill> { new() { Content = "You are a calculator agent." } });

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        int call = 0;

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .Tool(tool)
            .OnNativeBrain((messages, tools) =>
                ValueTask.FromResult(reply(tools, call++)));
    }

    [Fact]
    public async Task ShouldRoundTripANativeToolCallAsync()
    {
        // given — the model asks for the tool, then answers from its result
        var tool = new CalculatorTool();

        StandardAgent agent = AgentWith(tool, (tools, call) => call == 0
            ? new GenerationResult
            {
                ToolCalls =
                [
                    new ModelToolCall(
                        Id: "call_1",
                        Name: "calculator",
                        ArgumentsJson: """{"expression":"47*89"}""")
                ]
            }
            : new GenerationResult { Content = "4183" });

        // when
        string actualResult = await agent.ProcessPromptAsync("what is 47 times 89?");

        // then
        tool.ReceivedInputs.Should().ContainSingle();
        tool.ReceivedInputs[0].Should().Contain("47*89");
        actualResult.Should().Be("4183");
    }

    // The id is the whole point of native tool calling. A model that asked for call_1 expects the
    // answer back as a tool message naming call_1 — that is what it was trained on, and it is the
    // one thing the text protocol cannot express (SPEC.md §6). Replaying the result as narrated
    // prose leaves the model matching answers to questions by reading, which is exactly the
    // guessing native calls exist to remove.
    [Fact]
    public async Task ShouldReturnAToolResultTiedToTheCallThatAskedForItAsync()
    {
        // given
        var tool = new CalculatorTool();
        IReadOnlyList<ConversationMessage> secondTurn = [];

        StandardAgent agent = AgentWith(tool, (tools, call) => call == 0
            ? new GenerationResult
            {
                ToolCalls =
                [
                    new ModelToolCall("call_7", "calculator", """{"expression":"47*89"}""")
                ]
            }
            : new GenerationResult { Content = "4183" });

        agent = agent.OnNativeBrain((messages, tools) =>
        {
            if (messages.Any(message => message.Role is MessageRole.Tool))
            {
                secondTurn = messages;

                return ValueTask.FromResult(new GenerationResult { Content = "4183" });
            }

            return ValueTask.FromResult(new GenerationResult
            {
                ToolCalls =
                [
                    new ModelToolCall("call_7", "calculator", """{"expression":"47*89"}""")
                ]
            });
        });

        // when
        await agent.ProcessPromptAsync("what is 47 times 89?");

        // then — the assistant's request and the tool's answer are both present, and the answer
        // names the call it answers
        ConversationMessage? request = secondTurn.FirstOrDefault(message =>
            message.Role is MessageRole.Assistant && message.ToolCalls.Count > 0);

        ConversationMessage? answer =
            secondTurn.FirstOrDefault(message => message.Role is MessageRole.Tool);

        request.Should().NotBeNull();
        request!.ToolCalls[0].Id.Should().Be("call_7");

        answer.Should().NotBeNull();
        answer!.ToolCallId.Should().Be("call_7");
        answer.Content.Should().Be("4183");
    }

    private sealed class UndescribedTool : ITool
    {
        public string Name => "secret";
        public string Description => string.Empty;
        public string Parameters => "{}";

        public ValueTask<string> ExecuteAsync(string input) =>
            ValueTask.FromResult("never advertised");
    }

    [Fact]
    public async Task ShouldAdvertiseOnlyDescribedToolsNativelyAsync()
    {
        // given
        var tool = new CalculatorTool();
        IReadOnlyList<ToolDefinition> capturedTools = [];

        StandardAgent agent = AgentWith(tool, (tools, call) =>
        {
            capturedTools = tools;

            return new GenerationResult { Content = "done" };
        })
            .Tool(new UndescribedTool());

        // when
        await agent.ProcessPromptAsync("hello");

        // then — a description is the opt-in, natively exactly as in the text catalog: a tool
        // without one stays callable but is never offered to the model
        capturedTools.Should().Contain(definition => definition.Name == "calculator");
        capturedTools.Should().NotContain(definition => definition.Name == "secret");

        capturedTools.Should().OnlyContain(definition =>
            string.IsNullOrWhiteSpace(definition.Description) == false);
    }

    [Fact]
    public async Task ShouldReportNativeUsageForTheBudgetAsync()
    {
        // given — a model that reports what it cost, and a budget smaller than that
        var tool = new CalculatorTool();

        StandardAgent agent = AgentWith(tool, (tools, call) => new GenerationResult
        {
            ToolCalls =
            [
                new ModelToolCall("call_1", "calculator", """{"expression":"1+1"}""")
            ],

            PromptTokens = 500,
            CompletionTokens = 500
        })
            .MaxTurns(10)
            .Budget(maxTokens: 900);

        // when
        string actualResult = await agent.ProcessPromptAsync("loop");

        // then — reported usage is what the budget measures, not an estimate
        actualResult.Should().Contain("token budget");
    }
}
