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
using Standard.Agents.Tools;
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

    [Fact]
    public async Task ShouldReportARunOutOfTurnsAsTurnsExhaustedAsync()
    {
        // given
        StandardAgent agent =
            AgentThatLoops(turn => "ACTION: nonexistent_tool: keep going")
                .MaxTurns(2);

        // when
        AgentOutcome outcome = await agent.RunAsync("loop forever");

        // then
        // Still Working, because the run stopped mid-work, and now with the code that says so.
        outcome.Status.Should().Be(AgentStatus.Working);
        outcome.Failure.Should().NotBeNull();
        outcome.Failure!.Code.Should().Be(AgentFailureCodes.TurnsExhausted);
        outcome.Failure.Message.Should().Be(outcome.Result);
    }

    [Fact]
    public async Task ShouldStopARunGoingInCirclesAsync()
    {
        // given
        // Watched live, twice: a model asked for the same thing eleven more times with the note in
        // front of it, and a turn cap of sixty-four is sixty turns of the same question (SPEC.md
        // §4.10, v1.14). The act runs once; every ask after it is a replay.
        var tool = new CountingTool();

        StandardAgent agent =
            AgentThatLoops(turn => "ACTION: calculator: 1 + 1")
                .Tool(tool)
                .MaxTurns(20)
                .IdenticalCallLimit(3);

        // when
        AgentOutcome outcome = await agent.RunAsync("add one and one");

        // then
        // Asked, replayed, told, and then told the run is over: not a refusal and not an answer.
        tool.Calls.Should().Be(1);
        outcome.Status.Should().Be(AgentStatus.Failed);
        outcome.Failure.Should().NotBeNull();
        outcome.Failure!.Code.Should().Be(AgentFailureCodes.GoingInCircles);
        outcome.Failure.Message.Should().Be(outcome.Result);
    }

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

    [Fact]
    public async Task ShouldCarryNoFailureOnARunThatAnsweredAsync()
    {
        // given
        StandardAgent agent = AgentThatLoops(turn => "FINAL: 42");

        // when
        AgentOutcome outcome = await agent.RunAsync("what is the answer?");

        // then
        // An answer is not a stop without an answer, and must never look like one.
        outcome.Status.Should().Be(AgentStatus.Responded);
        outcome.Failure.Should().BeNull();
    }
}
