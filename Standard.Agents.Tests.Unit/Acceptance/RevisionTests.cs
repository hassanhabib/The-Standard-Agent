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

    // The 2026-08-23 sweep found the fix above was applied to one branch of one path. The root
    // cause — Interpret copies the incoming Status forward — has THREE exits, and resetting the
    // accepted-final-answer branch of ThinkAsync patched exactly one of them.
    //
    // Exit two: the streamed path. StreamThinkAsync has no reset at all, so a streamed draft
    // rejected once and accepted on revision still leaves Decision marked Revising, and the run
    // spins to the cap and refuses the answer that passed — the identical shipped bug, alive
    // behind the other door.
    [Fact]
    public async Task ShouldDeliverADraftThatPassesOnRevisionWhenStreamedAsync()
    {
        // given — the same judge as the batched test above
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
        List<string> responses = [];

        await foreach (var streamEvent in agent.StreamPromptAsync("what is the answer"))
        {
            if (streamEvent.Type == Models.Clients.Agents.AgentStreamEventType.Response)
            {
                responses.Add(streamEvent.Content);
            }
        }

        // then
        string.Concat(responses).Should().Contain(
            "forty two",
            because: "a revision that passes review is an answer on the streamed path too — "
                + "a control a caller can step around by changing method is not a control");

        judged.Should().Be(2);
    }

    // Exit three: the tool-call branch, on both paths. A model told its draft was rejected may
    // reasonably decide to consult a tool before drafting again — and the decided context for a
    // tool call carries the stale Revising out of Decision, so the loop skips Direction and the
    // call is silently swallowed. The recovery the revision loop exists to enable cannot route
    // through a tool.
    [Fact]
    public async Task ShouldExecuteAToolTheModelChoosesAfterARejectionAsync()
    {
        // given — reject the guess; the model then consults a tool and answers from it
        var tool = new RecordingTool();
        int judged = 0;
        int calls = 0;

        StandardAgent agent = new StandardAgent()
            .Tool(tool)
            .OnBrain((_, _) =>
            {
                calls++;

                return ValueTask.FromResult(calls switch
                {
                    1 => "FINAL: a guess",
                    2 => "ACTION: calculator: 47*89",
                    _ => "FINAL: the answer is 4183"
                });
            })
            .OnJudge((_, _) =>
            {
                judged++;

                return ValueTask.FromResult(judged == 1 ? "0.0" : "1.0");
            })
            .MaxTurns(5);

        // when
        string actualAnswer = await agent.ProcessPromptAsync("what is 47*89");

        // then
        tool.ExecutionCount.Should().Be(
            1,
            because: "the model chose to consult a tool after the rejection, and a revision "
                + "signal that swallows the act is a retry that cannot succeed that way");

        actualAnswer.Should().Be("the answer is 4183");
    }

    private sealed class RecordingTool : Standard.Agents.Tools.ITool
    {
        public string Name => "calculator";
        public string Description => "Evaluates arithmetic.";
        public int ExecutionCount { get; private set; }

        public ValueTask<string> ExecuteAsync(string input)
        {
            ExecutionCount++;

            return ValueTask.FromResult("4183");
        }
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
