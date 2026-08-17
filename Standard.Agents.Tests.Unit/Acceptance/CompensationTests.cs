// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Foundations.Skills;
using Standard.Agents.Models.Orchestrations.Effects;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// Compensation end to end (SPEC.md §4.9). Run-once makes an effect safe to PROPOSE twice; these
// pin what happens to the effects that cannot be made idempotent at all — the ones a bank asks
// about when the run dies halfway through a two-step payment.
public class CompensationTests
{
    // A tool that knows how to undo itself, and records the order it was called in.
    private sealed class ReversibleTool : ITool
    {
        private readonly List<string> ledger;

        public ReversibleTool(string name, List<string> ledger)
        {
            Name = name;
            this.ledger = ledger;
        }

        public string Name { get; }

        public string Description => $"Performs {Name}.";

        public string Parameters => "{}";

        public ValueTask<string> ExecuteAsync(string input)
        {
            this.ledger.Add($"do:{Name}");

            return ValueTask.FromResult($"{Name}-receipt");
        }

        public ValueTask<string?> CompensateAsync(string input, string outcome)
        {
            this.ledger.Add($"undo:{Name}");

            return ValueTask.FromResult<string?>($"reversed {outcome}");
        }
    }

    // A tool that cannot be undone — it leaves the default, which says so.
    private sealed class IrreversibleTool : ITool
    {
        private readonly List<string> ledger;

        public IrreversibleTool(string name, List<string> ledger)
        {
            Name = name;
            this.ledger = ledger;
        }

        public string Name { get; }

        public string Description => $"Performs {Name}.";

        public string Parameters => "{}";

        public ValueTask<string> ExecuteAsync(string input)
        {
            this.ledger.Add($"do:{Name}");

            return ValueTask.FromResult($"{Name}-sent");
        }
    }

    private static StandardAgent AgentThatRuns(IEnumerable<ITool> tools, params string[] replies)
    {
        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        var scriptedReplies = new Queue<string>(replies);

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .Tools(tools)
            .OnBrain(async (systemPrompt, userPrompt) =>
                scriptedReplies.Count > 0
                    ? scriptedReplies.Dequeue()
                    : "ACTION: book_flight: LHR-JFK");
    }

    [Fact]
    public async Task ShouldUnwindPerformedEffectsInReverseOrderWhenTheRunFailsAsync()
    {
        // given — the run books, then charges, then runs out of turns without ever answering
        List<string> ledger = [];

        StandardAgent agent = AgentThatRuns(
            [new ReversibleTool("book_flight", ledger),
                new ReversibleTool("charge_card", ledger)],
            "ACTION: book_flight: LHR-JFK",
            "ACTION: charge_card: 480.00")
            .MaxTurns(2)
            .CompensateOnFailure();

        // when
        string actualResult = await agent.ProcessPromptAsync(prompt: "book me a flight");

        // then — the charge is reversed before the booking it paid for
        ledger.Should().Equal("do:book_flight", "do:charge_card", "undo:charge_card",
            "undo:book_flight");

        actualResult.Should().Contain("Unwound 2 of 2 effects.");
    }

    [Fact]
    public async Task ShouldReportEffectsThatCouldNotBeUndoneAsync()
    {
        // given — one tool knows how to reverse itself and one does not
        List<string> ledger = [];

        StandardAgent agent = AgentThatRuns(
            [new ReversibleTool("book_flight", ledger),
                new IrreversibleTool("send_email", ledger)],
            "ACTION: book_flight: LHR-JFK",
            "ACTION: send_email: your ticket is confirmed")
            .MaxTurns(2)
            .CompensateOnFailure();

        // when
        string actualResult = await agent.ProcessPromptAsync(prompt: "book me a flight");

        // then — silence is not reversal; the caller is told what stands
        actualResult.Should().Contain("Unwound 1 of 2 effects.");
        actualResult.Should().Contain("'send_email' could not be undone; the effect stands.");
        ledger.Should().NotContain("undo:send_email");
    }

    [Fact]
    public async Task ShouldNotCompensateEffectsThatWereNeverPerformedAsync()
    {
        // given — policy denies the charge, so only the booking was ever performed
        List<string> ledger = [];

        StandardAgent agent = AgentThatRuns(
            [new ReversibleTool("book_flight", ledger),
                new ReversibleTool("charge_card", ledger)],
            "ACTION: book_flight: LHR-JFK",
            "ACTION: charge_card: 480.00")
            .MaxTurns(2)
            .CompensateOnFailure()
            .OnPolicy(effect => ValueTask.FromResult(
                effect.ToolName is "charge_card"
                    ? AuthorizationDecision.Deny("card charges are not permitted")
                    : AuthorizationDecision.Allow()));

        // when
        string actualResult = await agent.ProcessPromptAsync(prompt: "book me a flight");

        // then — a denied effect was never performed, so there is nothing of it to undo
        ledger.Should().Equal("do:book_flight", "undo:book_flight");
        actualResult.Should().Contain("Unwound 1 of 1 effects.");
    }

    [Fact]
    public async Task ShouldNotUnwindARunThatDeliveredAnAnswerAsync()
    {
        // given — the run performs an effect and then answers, which is a completed run
        List<string> ledger = [];

        StandardAgent agent = AgentThatRuns(
            [new ReversibleTool("book_flight", ledger)],
            "ACTION: book_flight: LHR-JFK",
            "FINAL: booked")
            .CompensateOnFailure();

        // when
        string actualResult = await agent.ProcessPromptAsync(prompt: "book me a flight");

        // then
        actualResult.Should().Be("booked");
        ledger.Should().Equal("do:book_flight");
    }

    [Fact]
    public async Task ShouldLeaveEffectsStandingWhenCompensationIsNotConfiguredAsync()
    {
        // given — compensation is off, which is the default
        List<string> ledger = [];

        StandardAgent agent = AgentThatRuns(
            [new ReversibleTool("book_flight", ledger)],
            "ACTION: book_flight: LHR-JFK")
            .MaxTurns(1);

        // when
        await agent.ProcessPromptAsync(prompt: "book me a flight");

        // then — behavior is exactly as if the section did not exist
        ledger.Should().Equal("do:book_flight");
    }

    [Fact]
    public async Task ShouldUnwindTheRemainingEffectsWhenOneCompensationFailsAsync()
    {
        // given — undoing the charge throws; the booking behind it must still be unwound
        List<string> ledger = [];

        StandardAgent agent = AgentThatRuns(
            [new ReversibleTool("book_flight", ledger), new ThrowingTool("charge_card", ledger)],
            "ACTION: book_flight: LHR-JFK",
            "ACTION: charge_card: 480.00")
            .MaxTurns(2)
            .CompensateOnFailure();

        // when
        string actualResult = await agent.ProcessPromptAsync(prompt: "book me a flight");

        // then — abandoning the rest would leave more standing, not less
        ledger.Should().Contain("undo:book_flight");
        actualResult.Should().Contain("Unwound 1 of 2 effects.");
        actualResult.Should().Contain("'charge_card' could not be undone; the effect stands.");
    }

    // A tool whose undo fails — the reversal itself is an act against the world, and acts fail.
    private sealed class ThrowingTool : ITool
    {
        private readonly List<string> ledger;

        public ThrowingTool(string name, List<string> ledger)
        {
            Name = name;
            this.ledger = ledger;
        }

        public string Name { get; }

        public string Description => $"Performs {Name}.";

        public string Parameters => "{}";

        public ValueTask<string> ExecuteAsync(string input)
        {
            this.ledger.Add($"do:{Name}");

            return ValueTask.FromResult($"{Name}-receipt");
        }

        public ValueTask<string?> CompensateAsync(string input, string outcome) =>
            throw new InvalidOperationException("the reversal window has closed");
    }
}
