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

// A budget that only applies to one protocol is not a budget.
//
// Token spend was read from what the provider REPORTED, and only the native V1 path has a report
// to read. On the text protocol PromptTokens and CompletionTokens stayed zero, the run added zero
// to its spend every turn, and .Budget(maxTokens:) / .Budget(maxCostUsd:) never tripped — while
// the Enterprise profile went on claiming budgets. V0 is not deprecated; it is the contract that
// works against any endpoint, which makes it the likelier half of the estate to be running.
//
// Measuring is Decision's (a token is the model's unit). Deciding whether to continue is the
// loop's. These tests pin the seam from the outside, through the builder, because that is the
// only place a caller can tell whether the control they configured actually exists.
public class BudgetTests
{
    private sealed class CountingTool : ITool
    {
        public string Name => "calculator";
        public string Description => "Evaluates arithmetic.";
        public int Calls { get; private set; }

        public ValueTask<string> ExecuteAsync(string input)
        {
            Calls++;

            return ValueTask.FromResult("2");
        }
    }

    private static StandardAgent AgentWith(
        Func<string, string, ValueTask<string>> brain,
        ITool tool)
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

    // A brain that never finishes: every turn asks for the tool again, so only a budget or the
    // turn cap can end the run. With the budget far below what the exchange costs, the budget
    // has to be what ends it.
    private static ValueTask<string> AlwaysCallsTheTool(string systemPrompt, string userPrompt) =>
        ValueTask.FromResult("ACTION: calculator: 1+1");

    [Fact]
    public async Task ShouldExhaustTheTokenBudgetOnTheTextProtocolAsync()
    {
        // given
        var tool = new CountingTool();

        StandardAgent agent = AgentWith(AlwaysCallsTheTool, tool)
            .MaxTurns(20)
            .Budget(maxTokens: 40);

        // when
        string actualResult = await agent.ProcessPromptAsync("what is one plus one");

        // then
        actualResult.Should().Contain(
            "token budget",
            because: "a text-protocol run consumes tokens like any other, so the budget it was "
                + "given has to be the thing that stops it");

        tool.Calls.Should().BeLessThan(
            20,
            because: "the run must stop on the budget rather than run out the turn cap");
    }

    [Fact]
    public async Task ShouldExhaustTheCostBudgetOnTheTextProtocolAsync()
    {
        // given
        var tool = new CountingTool();

        StandardAgent agent = AgentWith(AlwaysCallsTheTool, tool)
            .MaxTurns(20)
            .Budget(maxCostUsd: 0.0001m, costPerThousandTokens: 1m);

        // when
        string actualResult = await agent.ProcessPromptAsync("what is one plus one");

        // then
        actualResult.Should().Contain(
            "cost budget",
            because: "cost is priced off the token count, so a protocol that reports no tokens "
                + "reports no cost — and the bound silently never applies");
    }

    // The default is wide open: counting always happens, blocking does not. An agent given no
    // budget must not acquire one by having become measurable.
    [Fact]
    public async Task ShouldNotStopARunThatWasGivenNoBudgetAsync()
    {
        // given
        var tool = new CountingTool();
        StandardAgent agent = AgentWith(AlwaysCallsTheTool, tool).MaxTurns(3);

        // when
        string actualResult = await agent.ProcessPromptAsync("what is one plus one");

        // then
        actualResult.Should().NotContain("budget");
        tool.Calls.Should().BeGreaterThan(0);
    }
}
