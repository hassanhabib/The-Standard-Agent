// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Agents;

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
}

/// <summary>One exchange: what was asked, and what came back.</summary>
public sealed record AgentTurn(string Prompt, string Answer);
