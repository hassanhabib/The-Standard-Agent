// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Brokers.Effects;

/// <summary>
/// One act's line in the ledger: which act, who claimed it, when, for how long, where it stands,
/// and what it produced. Written before the act and updated after it, so an act that executed
/// and was never recorded is distinguishable from one that never ran (SPEC.md §4.9).
/// </summary>
public sealed record EffectRecord
{
    public string IdempotencyKey { get; init; } = "";
    public string ToolName { get; init; } = "";
    public EffectState State { get; init; }

    /// <summary>The run that claimed the act, so a repeat can tell its own attempt from another run's.</summary>
    public string Owner { get; init; } = "";
    public DateTimeOffset ClaimedOn { get; init; }

    /// <summary>
    /// Until when an in-flight claim may be presumed live. Past it, a claim with no outcome is
    /// an earlier attempt whose fate is unknown, and the ledger holds a repeat for reconciliation
    /// rather than presuming either that it happened or that it did not.
    /// </summary>
    public DateTimeOffset LeaseUntil { get; init; }
    public string? Outcome { get; init; }
    public string? Detail { get; init; }
    public DateTimeOffset? RecordedOn { get; init; }
}
