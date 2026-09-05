// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Sessions;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Coordinations.Agents.Exceptions;
using Standard.Agents.Models.Foundations.Skills;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// Conversation and resumption (SPEC.md §4.11).
//
// Invariant 4 says the agent INSTANCE is stateless across prompts. It does not say the
// conversation is. These pin that distinction: the same session continues across two separate
// agent instances, which is the property that makes resumption real rather than incidental.
public class SessionTests : IDisposable
{
    private readonly string sessionsPath =
        Path.Combine(Path.GetTempPath(), $"sessions-{Guid.NewGuid():n}");

    private StandardAgent NewAgent(Func<string, string> answerFor, Action<string>? capture = null)
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
            .OnBrain(async (systemPrompt, userPrompt) =>
            {
                capture?.Invoke(userPrompt);

                return answerFor(userPrompt);
            })
            .Sessions(this.sessionsPath);
    }

    [Fact]
    public async Task ShouldCarryConversationAcrossPromptsAsync()
    {
        // given
        string capturedSecondPrompt = string.Empty;
        int calls = 0;

        StandardAgent agent = NewAgent(
            answerFor: _ => ++calls == 1 ? "FINAL: Paris" : "FINAL: about 2.1 million",
            capture: userPrompt =>
            {
                if (calls == 1)
                {
                    capturedSecondPrompt = userPrompt;
                }
            });

        // when
        await agent.ProcessPromptAsync("what is the capital of France?", sessionId: "user-42");
        await agent.ProcessPromptAsync("and its population?", sessionId: "user-42");

        // then — the follow-up carries what came before
        capturedSecondPrompt.Should().Contain("capital of France");
        capturedSecondPrompt.Should().Contain("Paris");
    }

    // The property that makes resumption real: the session is the state, the instance is not.
    // An implementation whose continuity depends on the original instance still being alive has
    // not implemented this (SPEC.md §4.11).
    [Fact]
    public async Task ShouldResumeAConversationOnADifferentAgentInstanceAsync()
    {
        // given — the first agent answers, then is discarded entirely
        StandardAgent firstAgent = NewAgent(_ => "FINAL: Paris");
        await firstAgent.ProcessPromptAsync("what is the capital of France?", sessionId: "user-7");

        string capturedPrompt = string.Empty;

        StandardAgent secondAgent = NewAgent(
            answerFor: _ => "FINAL: about 2.1 million",
            capture: userPrompt => capturedPrompt = userPrompt);

        // when — a different instance picks the conversation up
        await secondAgent.ProcessPromptAsync("and its population?", sessionId: "user-7");

        // then
        capturedPrompt.Should().Contain("Paris");
    }

    [Fact]
    public async Task ShouldKeepSessionsSeparateAsync()
    {
        // given
        string capturedPrompt = string.Empty;

        StandardAgent agent = NewAgent(
            answerFor: _ => "FINAL: Paris",
            capture: userPrompt => capturedPrompt = userPrompt);

        await agent.ProcessPromptAsync("what is the capital of France?", sessionId: "alice");

        // when — a different conversation entirely
        await agent.ProcessPromptAsync("what is the capital of Japan?", sessionId: "bob");

        // then — bob never sees alice's exchange
        capturedPrompt.Should().NotContain("France");
    }

    [Fact]
    public async Task ShouldNotRecordACancelledRunAsAnAnswerAsync()
    {
        // given
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        StandardAgent agent = NewAgent(_ => "FINAL: Paris");

        await agent.ProcessPromptAsync(
            "what is the capital of France?", sessionId: "user-9", cancellation.Token);

        string capturedPrompt = string.Empty;

        StandardAgent nextAgent = NewAgent(
            answerFor: _ => "FINAL: anything",
            capture: userPrompt => capturedPrompt = userPrompt);

        // when
        await nextAgent.ProcessPromptAsync("follow up", sessionId: "user-9");

        // then — the next prompt is never told the agent said something it never said
        capturedPrompt.Should().NotContain("cancelled");
        capturedPrompt.Should().NotContain("Paris");
    }

    [Fact]
    public async Task ShouldStayStatelessWithoutASessionAsync()
    {
        // given
        string capturedPrompt = string.Empty;
        int calls = 0;

        StandardAgent agent = NewAgent(
            answerFor: _ => ++calls == 1 ? "FINAL: Paris" : "FINAL: anything",
            capture: userPrompt =>
            {
                if (calls == 1)
                {
                    capturedPrompt = userPrompt;
                }
            });

        // when — no session id at all
        await agent.ProcessPromptAsync("what is the capital of France?");
        await agent.ProcessPromptAsync("and its population?");

        // then — absent a session the agent is exactly as stateless as it always was
        capturedPrompt.Should().NotContain("Paris");
    }

    public void Dispose()
    {
        if (Directory.Exists(this.sessionsPath))
        {
            Directory.Delete(this.sessionsPath, recursive: true);
        }
    }

    // Found in the 2026-09-04 principal review (F-06): a session id is caller-provided and the
    // record carried no owner, so two callers sharing or guessing one id shared one history. A
    // session records the principal that opened it, and a different principal is refused as a
    // validation failure, never served a quietly shared conversation.
    [Fact]
    public async Task ShouldRefuseASessionOwnedByAnotherPrincipalAsync()
    {
        // given
        StandardAgent adasAgent = NewAgent(answerFor: _ => "FINAL: noted").Principal(() => "ada");
        StandardAgent bobsAgent = NewAgent(answerFor: _ => "FINAL: noted").Principal(() => "bob");
        await adasAgent.ProcessPromptAsync("remember my order", sessionId: "order-42");

        // when
        ValueTask<string> bobsTask =
            bobsAgent.ProcessPromptAsync("what was the order?", sessionId: "order-42");

        // then
        await Assert.ThrowsAsync<AgentCoordinationValidationException>(bobsTask.AsTask);
    }

    // Found in the 2026-09-04 principal review (F-06): a save was read, append, whole-record
    // upsert, so two prompts in one session could both read the old history and the last writer
    // erased the other's completed turn. Every write carries the version it was based on, the
    // store refuses a stale one, and the loop re-reads and tries again.
    [Fact]
    public async Task ShouldKeepEveryTurnWhenPromptsRunConcurrentlyInOneSessionAsync()
    {
        // given
        StandardAgent agent = NewAgent(answerFor: _ => "FINAL: noted");

        IReadOnlyList<string> prompts =
            [.. Enumerable.Range(0, 8).Select(index => $"fact number {index}")];

        // when
        await Task.WhenAll(prompts.Select(prompt =>
            agent.ProcessPromptAsync(prompt, sessionId: "busy-session").AsTask()));

        // then: every prompt's turn is in the history, whichever order they landed in
        AgentSession? session =
            await new FileSessionBroker(this.sessionsPath).SelectSessionAsync("busy-session");

        session.Should().NotBeNull();
        session!.History.Select(turn => turn.Prompt).Should().BeEquivalentTo(prompts);
    }
}
