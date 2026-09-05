// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents.Services.Managements;

// Conversation and resumption (SPEC.md §4.11).
//
// Invariant 4 still holds: the instance is stateless across prompts. What persists is the
// session, and it lives in a broker outside the agent — which is what lets a pause be resumed by
// a different process, long after the instance that created it is gone.
public partial class RunManagementService
{
    // Bounded, because a store that refuses every write is an outage, not contention.
    private const int MaxSessionWriteAttempts = 16;

    // Read before the run begins, because a resumed run must keep the identity the interrupted
    // one had. Nothing is logged here: there is no run yet to credit the record to. A session
    // another principal opened is refused here, before a line of their conversation is read.
    private async ValueTask<AgentSession?> PeekSessionAsync(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            return null;
        }

        AgentSession? session = await this.dataCoordinationService.RecallSessionAsync(sessionId);
        ValidateSessionOwner(session, CurrentPrincipal());

        return session;
    }

    private string CurrentPrincipal() =>
        this.principalResolver?.Resolve()?.Id ?? string.Empty;

    // A session that never delivered an answer was interrupted — killed, cancelled, out of turns,
    // or waiting on an authority. The next prompt in that session continues that run rather than
    // starting a fresh one, so the effects it already performed keep their idempotency keys and
    // are replayed rather than performed twice (SPEC.md §4.9, §4.11).
    private static string? ResumedRunId(AgentSession? session) =>
        session is not null
            && string.IsNullOrEmpty(session.RunId) is false
            && Delivered(session.Status) is false
                ? session.RunId
                : null;

    // Delivered means the caller got a conclusion — an answer, or a refusal that is itself an
    // answer. Everything else left the run in the middle of something.
    private static bool Delivered(AgentStatus status) =>
        status is AgentStatus.Responded or AgentStatus.Refused;

    // The start-of-run checkpoint. Written before any work, because a crash means nothing at the
    // end runs at all — and an identity recorded only on success is an identity the failure case
    // can never use.
    private async ValueTask BeginSessionAsync(AgentContext context)
    {
        if (string.IsNullOrEmpty(context.SessionId))
        {
            return;
        }

        // History is carried through untouched. The checkpoint records who is working the
        // session, not what was said — this prompt has no answer yet, and writing one here
        // would tell the next prompt the agent said something it never said.
        await RecordSessionWithRetryAsync(context.SessionId, existing =>
            new AgentSession
            {
                Id = context.SessionId,
                History = existing?.History ?? [],
                Status = AgentStatus.Working,
                PendingQuestion = existing?.PendingQuestion ?? string.Empty,
                RunId = AgentRun.Current?.Id ?? string.Empty
            });
    }

    // Every write is based on a fresh read and says so: the version it was read at plus one,
    // and the owner the record already had or the principal writing it now. A store that honors
    // versions refuses a write based on a read that is no longer current — another prompt in
    // the same session wrote first — and the loop reads again and tries again, so no completed
    // turn is erased by a slower writer (SPEC.md §4.11; principal review 2026-09-04, F-06).
    private async ValueTask RecordSessionWithRetryAsync(
        string sessionId,
        Func<AgentSession?, AgentSession> compose)
    {
        for (int attempt = 1; ; attempt++)
        {
            AgentSession? existing =
                await this.dataCoordinationService.RecallSessionAsync(sessionId);

            AgentSession session = compose(existing) with
            {
                Owner = existing is { Owner.Length: > 0 } ? existing.Owner : CurrentPrincipal(),
                Version = (existing?.Version ?? 0) + 1
            };

            try
            {
                await this.dataCoordinationService.RecordSessionAsync(session);

                return;
            }
            catch (Exception exception)
                when (IsStaleSessionWrite(exception) && attempt < MaxSessionWriteAttempts)
            {
                await this.loggingBroker.LogProcessAsync(
                    "Run",
                    $"Session '{sessionId}' moved on since it was read; reading it again "
                        + $"(attempt {attempt})",
                    detail: true);
            }
        }
    }

    // The refusal arrives wrapped by every tier it crossed; what it IS sits at the bottom.
    private static bool IsStaleSessionWrite(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is Models.Foundations.Sessions.Exceptions.StaleSessionException)
            {
                return true;
            }
        }

        return false;
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

        await RecordSessionWithRetryAsync(context.SessionId, existing =>
            new AgentSession
            {
                Id = context.SessionId,
                History = [.. existing?.History ?? [], new AgentTurn(context.Prompt, context.Result)],
                Status = context.Status,
                RunId = AgentRun.Current?.Id ?? string.Empty,

                // What the agent is waiting on, so a later prompt can see it was mid-question
                // rather than mid-nothing.
                PendingQuestion = context.Status is AgentStatus.AwaitingInput
                    or AgentStatus.AwaitingApproval
                        ? context.Result
                        : string.Empty,

                // Set only by the branch that holds an act, and the context belongs to this run
                // alone — so a run that did not hold anything carries nothing, and the session
                // stops advertising a held act as soon as one is permitted and performed.
                PendingEffect = context.PendingEffect
            });
    }
}
