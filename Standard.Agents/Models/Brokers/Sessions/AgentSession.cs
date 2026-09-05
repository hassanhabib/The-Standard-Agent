// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Models.Brokers.Sessions;

/// <summary>
/// One conversation: what was said, and what it was left in the middle of.
/// </summary>
/// <remarks>
/// Invariant 4 says the agent <i>instance</i> is stateless across prompts. It does not say the
/// conversation is (SPEC.md §4.11). The session is where continuity lives — outside the agent, in
/// a broker — which is what lets a pause be resumed by a different process, or by a different
/// machine, long after the instance that created it is gone.
/// </remarks>
public sealed record AgentSession
{
    public string Id { get; init; } = "";

    /// <summary>What was said before, oldest first.</summary>
    public IReadOnlyList<AgentTurn> History { get; init; } = [];

    /// <summary>What the session was left in the middle of, if anything.</summary>
    public AgentStatus Status { get; init; }

    /// <summary>The question the agent asked and is waiting on, when awaiting input.</summary>
    public string PendingQuestion { get; init; } = "";

    /// <summary>
    /// The act this session is waiting on an authority to permit, when awaiting approval
    /// (SPEC.md §4.11, §4.9).
    /// </summary>
    /// <remarks>
    /// The act travels with the session, not just the fact that something is waiting. A process
    /// that picks the session up has to be able to show a human <i>what</i> they are approving,
    /// and an approval broker has to be able to check that the act it is now being asked about is
    /// the act that was held — its <c>IdempotencyKey</c> is what makes that checkable.
    /// </remarks>
    public AgentEffect? PendingEffect { get; init; }

    /// <summary>
    /// The run that last worked this session, written when the run <i>starts</i>.
    /// </summary>
    /// <remarks>
    /// Written at the start rather than at the end because a crash means nothing at the end runs
    /// at all. It is what lets a resumed run keep the identity the interrupted one had — and
    /// therefore derive the same idempotency keys (SPEC.md §4.9), so an effect that already went
    /// out is replayed rather than performed a second time.
    /// </remarks>
    public string RunId { get; init; } = "";

    /// <summary>
    /// The principal that opened this session, stamped on the first write and kept. A session
    /// id is caller-provided, so without an owner two callers sharing or guessing one id share
    /// one history; with one, a different principal is refused. Empty when no principal was
    /// configured, which is the anonymous, shared-by-id behavior that existed before.
    /// </summary>
    public string Owner { get; init; } = "";

    /// <summary>
    /// The version this record was written as, one more than the version it was read at. A
    /// store that honors it refuses a write based on a stale read, so two prompts in one session
    /// at once cannot erase each other's completed turn; the loop re-reads and tries again.
    /// Zero for a record written before versions existed.
    /// </summary>
    public long Version { get; init; }
}

/// <summary>One exchange: what was asked, and what came back.</summary>
public sealed record AgentTurn(string Prompt, string Answer);
