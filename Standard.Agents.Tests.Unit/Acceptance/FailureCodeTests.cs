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
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// Why a run stopped without an answer, as a code (SPEC.md §3.6, v1.14).
//
// A status says that a run did not answer; it does not say why, and a caller cannot tell "retry
// this" from "change the prompt" from a sentence it would have to parse. These pin each stop's code
// from the outside, through RunAsync, because that is where a caller reads it.
public class FailureCodeTests
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

    [Fact]
    public async Task ShouldReportACancelledRunAsCancelledAsync()
    {
        // given
        using var cancellation = new CancellationTokenSource();

        StandardAgent agent = AgentThatLoops(turn =>
        {
            cancellation.Cancel();

            return "ACTION: nonexistent_tool: keep going";
        });

        // when
        AgentOutcome outcome = await agent.RunAsync("loop forever", cancellation.Token);

        // then
        outcome.Status.Should().Be(AgentStatus.Failed);
        outcome.Failure.Should().NotBeNull();
        outcome.Failure!.Code.Should().Be(AgentFailureCodes.Cancelled);
        outcome.Failure.Category.Should().Be(AgentFailureCategory.Service);
        outcome.Failure.Message.Should().Be(outcome.Result);
    }

    [Fact]
    public async Task ShouldReportAnExhaustedBudgetAsBudgetExhaustedAsync()
    {
        // given
        StandardAgent agent =
            AgentThatLoops(turn => "ACTION: nonexistent_tool: keep going")
                .MaxTurns(50)
                .Budget(maxWallClock: TimeSpan.Zero);

        // when
        AgentOutcome outcome = await agent.RunAsync("loop forever");

        // then
        outcome.Status.Should().Be(AgentStatus.Failed);
        outcome.Failure.Should().NotBeNull();
        outcome.Failure!.Code.Should().Be(AgentFailureCodes.BudgetExhausted);
        outcome.Failure.Message.Should().Be(outcome.Result);
    }
}
