// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Brokers.Effects;

/// <summary>
/// Where an act stands in the ledger (SPEC.md §4.9). The states are typed because the window
/// between "about to run" and "recorded" used to be a string, and a process that died inside it
/// left a claim every later proposal read as a result (principal review 2026-09-04, F-08).
/// </summary>
public enum EffectState
{
    /// <summary>Claimed; the act is running, or a run died before it could say how it ended.</summary>
    InFlight,

    /// <summary>Performed, and its outcome is on the record; a repeat replays it.</summary>
    Completed,

    /// <summary>The tool threw. Whether the world changed is unknown; a repeat is held.</summary>
    Failed,

    /// <summary>Compensation was decided and recorded before it was attempted.</summary>
    CompensationPending,

    /// <summary>The act was undone by its compensating act.</summary>
    Compensated
}
