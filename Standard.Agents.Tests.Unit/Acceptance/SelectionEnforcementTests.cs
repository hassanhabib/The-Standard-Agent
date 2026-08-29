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
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Foundations.Skills;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// Enforced selection (SPEC.md §4.15): selection narrows what a run is SHOWN, and a brain the
// loop fully mediates can only call what it was shown. But a brain is configuration — a custom
// brain, a gateway, a model router — and configuration can carry side-channel knowledge of the
// catalog. With enforcement on, the offering also BINDS at the Direction perimeter: an act
// naming an advertised tool the run was not offered is denied — told, non-terminal,
// recoverable — exactly as a policy denial is. Off by default; the §4.15 treatment of an
// unoffered tool (reachable if the Brain names it) remains the unamended behavior.
public class SelectionEnforcementTests
{
    private sealed class NamedTool : ITool
    {
        private readonly string description;

        public NamedTool(string name, string description = null!)
        {
            Name = name;
            this.description = description ?? $"The {name} tool.";
        }

        public string Name { get; }
        public string Description => this.description;
        public string Parameters => """{"type":"object"}""";
        public int Executions { get; private set; }

        public ValueTask<string> ExecuteAsync(string input)
        {
            Executions++;

            return ValueTask.FromResult("ran");
        }
    }

    private static StandardAgent AgentWith(
        Func<IReadOnlyList<ConversationMessage>, IReadOnlyList<ToolDefinition>, GenerationResult> reply,
        params ITool[] tools)
    {
        var skillBroker = new Mock<ISkillBroker>();

        skillBroker.Setup(broker => broker.SelectSkillsAsync())
            .ReturnsAsync(new List<Skill> { new() { Content = "You are an assistant." } });

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        StandardAgent agent = new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .OnNativeBrain((messages, tools) => ValueTask.FromResult(reply(messages, tools)));

        foreach (ITool tool in tools)
        {
            agent = agent.Tool(tool);
        }

        return agent;
    }

    // A brain that names a tool on its first decision and answers on its second — the shape of
    // every disobedient-brain scenario below.
    private static Func<IReadOnlyList<ConversationMessage>, IReadOnlyList<ToolDefinition>, GenerationResult>
        CallsThenAnswers(string toolName, string answer, Action<IReadOnlyList<ConversationMessage>> onSecondTurn = null!)
    {
        int brainCalls = 0;

        return (messages, tools) =>
        {
            if (++brainCalls == 1)
            {
                return new GenerationResult
                {
                    Content = string.Empty,
                    ToolCalls = [new ModelToolCall("call_1", toolName, "{}")],
                };
            }

            onSecondTurn?.Invoke(messages);

            return new GenerationResult { Content = answer };
        };
    }

    [Fact]
    public async Task ShouldDenyAnUnofferedToolWhenTheOfferingIsEnforcedAsync()
    {
        // given — the run is offered only the calculator, yet the brain names web_search anyway
        var webSearch = new NamedTool("web_search");
        var calculator = new NamedTool("calculator");
        IReadOnlyList<ConversationMessage> secondTurnMessages = null!;

        StandardAgent agent = AgentWith(
            CallsThenAnswers(
                "web_search",
                "answered among what was offered",
                messages => secondTurnMessages = messages),
            webSearch, calculator)
            .OnSelectTools((task, described) =>
                new ValueTask<IReadOnlyList<string>>(new[] { "calculator" }))
            .EnforceSelection();

        // when
        string result = await agent.ProcessPromptAsync("what is 2 + 2?");

        // then — the act never ran, the denial was told, and the run recovered
        webSearch.Executions.Should().Be(0);
        result.Should().Be("answered among what was offered");

        secondTurnMessages.Should().Contain(message =>
            message.Content != null && message.Content.Contains("not offered"));
    }
}
