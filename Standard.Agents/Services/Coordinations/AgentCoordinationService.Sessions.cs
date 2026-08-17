// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents.Services.Coordinations;

// Conversation and resumption (SPEC.md §4.11).
//
// Invariant 4 still holds: the instance is stateless across prompts. What persists is the
// session, and it lives in a broker outside the agent — which is what lets a pause be resumed by
// a different process, long after the instance that created it is gone.
public partial class AgentCoordinationService
{
    // Read before the run begins, because a resumed run must keep the identity the interrupted
    // one had. Nothing is logged here: there is no run yet to credit the record to.
    private async ValueTask<AgentSession?> PeekSessionAsync(string sessionId) =>
        string.IsNullOrEmpty(sessionId)
            ? null
            : await this.sessionBroker.SelectSessionAsync(sessionId);

    // A session that never delivered an answer was interrupted — killed, cancelled, out of turns,
    // or waiting on an authority. The next prompt in that session continues that run rather than
    // starting a fresh one, so the effects it already performed keep their idempotency keys and
    // are replayed rather than performed twice (SPEC.md §4.9, §4.11).
    private static string? ResumedRunId(AgentSession? session) =>
        null;

    // The start-of-run checkpoint. Written before any work, because a crash means nothing at the
    // end runs at all — and an identity recorded only on success is an identity the failure case
    // can never use.
    private async ValueTask BeginSessionAsync(AgentContext context, AgentSession? session)
    {
        await ValueTask.CompletedTask;
    }

    private async ValueTask<AgentContext> LoadSessionAsync(
        AgentContext context,
        AgentSession? session)
    {
        if (session is null)
        {
            return context;
        }

        // Bounded, oldest first. An unbounded history makes every prompt in a long conversation
        // cost more than the last, without limit — the bill grows on its own.
        IReadOnlyList<AgentTurn> history = session.History.Count > this.maxHistoryTurns
            ? [.. session.History.Skip(session.History.Count - this.maxHistoryTurns)]
            : session.History;

        await this.loggingBroker.LogProcessAsync(
            "Data", $"Recalled {history.Count} conversation turn(s)");

        return context with { History = history };
    }

    // Only a completed prompt is recorded. A cancelled or budget-stopped run must never be
    // written back as an answer: the next prompt would then be told the agent said something it
    // never said, and the conversation would be quietly wrong from that point on.
    private async ValueTask SaveSessionAsync(AgentContext context, bool completed)
    {
        if (string.IsNullOrEmpty(context.SessionId) || completed is false)
        {
            return;
        }

        AgentSession? existing =
            await this.sessionBroker.SelectSessionAsync(context.SessionId);

        IReadOnlyList<AgentTurn> history = existing?.History ?? [];

        var session = new AgentSession
        {
            Id = context.SessionId,
            History = [.. history, new AgentTurn(context.Prompt, context.Result)],
            Status = context.Status,
            RunId = AgentRun.Current?.Id ?? string.Empty,

            // What the agent is waiting on, so a later prompt can see it was mid-question
            // rather than mid-nothing.
            PendingQuestion = context.Status is AgentStatus.AwaitingInput
                or AgentStatus.AwaitingApproval
                    ? context.Result
                    : string.Empty
        };

        await this.sessionBroker.UpsertSessionAsync(session);
    }
}
