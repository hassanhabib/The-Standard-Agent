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

    // Off by default: without enforcement, an unoffered tool keeps its §4.15 treatment —
    // reachable if the Brain names it — so no existing deployment changes behavior.
    [Fact]
    public async Task ShouldKeepAnUnofferedToolReachableWithoutEnforcementAsync()
    {
        // given — the same disobedient brain, with no enforcement configured
        var webSearch = new NamedTool("web_search");
        var calculator = new NamedTool("calculator");

        StandardAgent agent = AgentWith(
            CallsThenAnswers("web_search", "done"),
            webSearch, calculator)
            .OnSelectTools((task, described) =>
                new ValueTask<IReadOnlyList<string>>(new[] { "calculator" }));

        // when
        string result = await agent.ProcessPromptAsync("what is 2 + 2?");

        // then — the unamended behavior: the named tool ran
        webSearch.Executions.Should().Be(1);
        result.Should().Be("done");
    }

    // Enforcement takes effect only when a selector recorded an offering: with no selector
    // there is nothing to enforce, and every described tool remains offered and callable.
    [Fact]
    public async Task ShouldNotDenyAnythingWhenNoSelectorIsConfiguredAsync()
    {
        // given — enforcement on, but no selection judgment supplied
        var webSearch = new NamedTool("web_search");

        StandardAgent agent = AgentWith(
            CallsThenAnswers("web_search", "done"),
            webSearch)
            .EnforceSelection();

        // when
        string result = await agent.ProcessPromptAsync("look something up");

        // then
        webSearch.Executions.Should().Be(1);
        result.Should().Be("done");
    }

    // A name selection never saw is not selection's to withhold: an undescribed tool keeps its
    // §6.1 treatment — callable if the Brain names it, never advertised — under enforcement too.
    [Fact]
    public async Task ShouldNotDenyAnUndescribedToolAsync()
    {
        // given — a tool with no description (selection operates over described names only) and
        // a selector that offers nothing
        var undescribed = new NamedTool("secret_helper", description: "");

        StandardAgent agent = AgentWith(
            CallsThenAnswers("secret_helper", "done"),
            undescribed)
            .OnSelectTools((task, described) =>
                new ValueTask<IReadOnlyList<string>>(Array.Empty<string>()))
            .EnforceSelection();

        // when
        string result = await agent.ProcessPromptAsync("hello");

        // then
        undescribed.Executions.Should().Be(1);
        result.Should().Be("done");
    }

    // Caller tools are the caller's own vocabulary and are never subject to selection
    // (SPEC.md §4.15): under enforcement, a call naming one still goes back to the caller as a
    // pending effect — classified before the perimeter, so there is nothing here to deny.
    [Fact]
    public async Task ShouldNeverDenyACallerToolAsync()
    {
        // given — enforcement on and an offering of nothing; the brain names the CALLER's tool
        StandardAgent agent = AgentWith(
            (messages, tools) => new GenerationResult
            {
                Content = string.Empty,
                ToolCalls = [new ModelToolCall("call_9", "send_email", """{"to":"jane"}""")],
            })
            .OnSelectTools((task, described) =>
                new ValueTask<IReadOnlyList<string>>(Array.Empty<string>()))
            .EnforceSelection();

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

        // then — the run paused for the caller rather than denying or refusing
        outcome.Status.Should().Be(AgentStatus.AwaitingInput);
        outcome.PendingEffect.Should().NotBeNull();
        outcome.PendingEffect!.ToolName.Should().Be("send_email");
    }
}
