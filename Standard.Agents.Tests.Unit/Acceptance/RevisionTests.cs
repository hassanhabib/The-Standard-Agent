// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// A rejected draft is a re-think signal, not a fault — the loop retries within the turn budget and
// refuses gracefully only when the answer still cannot pass. That is what 1.2.0.0 said it built.
//
// The retry half was covered. The SUCCESS half was not: every test asserted that a permanently
// rejected draft ends in a refusal, and none asserted that a draft rejected once and then accepted
// is the answer the caller receives.
//
// It never was. Interpret carries the incoming context forward with `context with { ... }`, and
// nothing resets Status, so a draft accepted on the second pass is still marked Revising when it
// leaves Decision. The loop sees Revising, continues, and the run exhausts its turns — returning
// "I can't help with that at the moment" for an answer that passed review.
public class RevisionTests
{
    [Fact]
    public async Task ShouldDeliverADraftThatPassesOnRevisionAsync()
    {
        // given — the Judge rejects the first draft and accepts every one after it
        int judged = 0;

        StandardAgent agent = new StandardAgent()
            .OnBrain((_, _) => ValueTask.FromResult("FINAL: forty two"))
            .OnJudge((_, _) =>
            {
                judged++;

                return ValueTask.FromResult(judged == 1 ? "0.0" : "1.0");
            })
            .MaxTurns(4);

        // when
        string actualAnswer = await agent.ProcessPromptAsync("what is the answer");

        // then
        actualAnswer.Should().Be(
            "forty two",
            because: "a revision that passes review is an answer, and a loop that cannot deliver "
                + "one has a retry it can never succeed at");

        judged.Should().Be(
            2,
            because: "one rejection and one acceptance is the whole exchange — anything more is "
                + "the loop spinning after the answer was already good");
    }

    // The other half, which was already covered and must stay true: a draft that never passes ends
    // in a graceful refusal rather than an exception or an empty string.
    [Fact]
    public async Task ShouldRefuseWhenNoDraftEverPassesAsync()
    {
        // given
        StandardAgent agent = new StandardAgent()
            .OnBrain((_, _) => ValueTask.FromResult("FINAL: forty two"))
            .OnJudge((_, _) => ValueTask.FromResult("0.0"))
            .MaxTurns(3);

        // when
        string actualAnswer = await agent.ProcessPromptAsync("what is the answer");

        // then
        actualAnswer.Should().Be("I can't help with that at the moment.");
    }
}
