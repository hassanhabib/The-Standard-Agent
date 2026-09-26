// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Models.Loggings;

/// <summary>
/// An act this run actually performed, and what it produced. Recorded so the run can be unwound
/// (SPEC.md §4.9): compensation needs the arguments the tool was called with <i>and</i> the outcome
/// it returned, because the outcome carries the identity the undo has to name.
/// </summary>
/// <remarks>
/// Only performed acts are kept. An effect denied by policy, held for approval, or replayed from
/// the ledger was never performed by this run, and compensating it would undo something this run
/// did not do.
/// </remarks>
public sealed record PerformedEffect(string ToolName, string Arguments, string Outcome)
{
    /// <summary>
    /// The act's line in the ledger, so compensating it can be recorded against the same record
    /// that says it happened (SPEC.md §4.9). Empty when the act was performed without a ledger.
    /// </summary>
    public string IdempotencyKey { get; init; } = "";

    /// <summary>
    /// What the act touched, as its tool named it, so a later look at the same place can be told
    /// from the look before it (SPEC.md §4.9, v1.14). Empty when the tool names nothing.
    /// </summary>
    public string Scope { get; init; } = "";

    /// <summary>
    /// How consequential the act was. Only an act that is not Safe changes what a later look at its
    /// scope would see.
    /// </summary>
    public RiskLevel RiskLevel { get; init; } = RiskLevel.Safe;
}
