// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Standard.Agents.Brokers.Usages;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// Usage, as it is spent (SPEC.md §4.14.1). The loop has always counted what a run spends, because
// the budget cannot bound what it does not count, and it kept the count to itself: somebody
// watching a run could read a clock and nothing else. A local model thinking for four minutes and a
// paid one spending forty thousand tokens looked the same until each of them ended.
//
// So after every model call the stream says what the run has spent so far, the same count the
// budget reads, marked estimated where nobody reported it.
public class UsageStreamTests
{
    private sealed class ScriptedTool : ITool
    {
        private readonly string output;

        public ScriptedTool(string name, string output)
        {
            Name = name;
            this.output = output;
        }

        public string Name { get; }
        public string Description => "A scripted tool.";

        public async ValueTask<string> ExecuteAsync(string input) => this.output;
    }

    // Every text the same size, so every call costs the same and a running total is a count of
    // calls. With the real estimate the second call always costs more than the first, because it
    // carries the first one's result, and a stream reporting each call's own figure would grow
    // just the same.
    private sealed class FlatUsageBroker : IUsageBroker
    {
        public async ValueTask<int> CountTokensAsync(string text) =>
            string.IsNullOrEmpty(text) ? 0 : 10;
    }

    private static async ValueTask<List<AgentStreamEvent>> DrainAsync(
        StandardAgent agent, string prompt)
    {
        List<AgentStreamEvent> events = [];

        await foreach (AgentStreamEvent streamEvent in agent.StreamPromptAsync(prompt))
        {
            events.Add(streamEvent);
        }

        return events;
    }

    [Fact]
    public async Task ShouldSayWhatTheRunHasSpentAfterEveryModelCallAsync()
    {
        // given
        int calls = 0;

        StandardAgent agent = new StandardAgent()
            .OnBrain(async (_, _) => ++calls == 1 ? "ACTION: calculator: 2+2" : "FINAL: 4")
            .Tool(new ScriptedTool("calculator", "4"))
            .UseUsage(new FlatUsageBroker());

        // when
        List<AgentStreamEvent> events = await DrainAsync(agent, "what is 2+2?");

        // then
        List<AgentStreamEvent> usages =
            [.. events.Where(streamEvent => streamEvent.Type == AgentStreamEventType.Usage)];

        usages.Should().HaveCount(
            calls,
            because: "one model call to act and one to answer is two things spent, and somebody "
                + "watching is owed each of them as it happens");

        usages[0].Usage!.TotalTokens.Should().BeGreaterThan(0);

        usages[1].Usage!.TotalTokens.Should().Be(
            usages[0].Usage!.TotalTokens * 2,
            because: "it is what the run has spent so far rather than what the last call cost, so a "
                + "consumer that missed an event still reads the right number from the next");

        usages.Should().OnlyContain(
            usage => usage.Content == usage.Usage!.TotalTokens.ToString(),
            because: "a consumer that reads only the text still reads the number");

        usages.Should().OnlyContain(
            usage => usage.Usage!.IsEstimated,
            because: "a brain that reports nothing was counted here, and an estimate presented as "
                + "a measurement is the one thing SPEC.md §3.4 forbids outright");

        string answer = string.Concat(events
            .Where(streamEvent => streamEvent.Type == AgentStreamEventType.Response)
            .Select(streamEvent => streamEvent.Content));

        answer.Should().Be("4", because: "what a run spent is not part of what it answered");
    }

    [Fact]
    public async Task ShouldCountTheDraftThatWasSentBackAsync()
    {
        // given
        int judged = 0;

        StandardAgent agent = new StandardAgent()
            .OnBrain(async (_, _) => "FINAL: forty two")
            .OnJudge(async (_, _) => ++judged == 1 ? "0.0" : "1.0")
            .MaxTurns(4);

        // when
        List<AgentStreamEvent> events = await DrainAsync(agent, "what is the answer");

        // then
        events.Count(streamEvent => streamEvent.Type == AgentStreamEventType.Usage).Should().Be(
            2,
            because: "a draft the Judge sent back was still written, and the tokens it took were "
                + "spent whether or not anybody reads it");
    }
}
