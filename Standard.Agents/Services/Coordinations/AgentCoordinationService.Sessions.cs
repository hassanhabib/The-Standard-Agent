// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents.Services.Coordinations;

// Conversation and resumption (SPEC.md §4.11).
//
// Invariant 4 still holds: the instance is stateless across prompts. What persists is the
// session, and it lives in a broker outside the agent — which is what lets a pause be resumed by
// a different process, long after the instance that created it is gone.
public partial class AgentCoordinationService
{
    private async ValueTask<AgentContext> LoadSessionAsync(AgentContext context)
    {
        if (string.IsNullOrEmpty(context.SessionId))
        {
            return context;
        }

        AgentSession? session =
            await this.sessionBroker.SelectSessionAsync(context.SessionId);

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
