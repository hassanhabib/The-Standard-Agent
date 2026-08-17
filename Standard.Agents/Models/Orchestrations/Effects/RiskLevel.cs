// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Orchestrations.Effects;

/// <summary>
/// How much an act costs to get wrong. <see cref="Safe"/> is the default, so an agent that
/// adopts effects without classifying its tools behaves exactly as it did before (SPEC.md §3.3).
/// </summary>
public enum RiskLevel
{
    /// <summary>Reversible and cheap — a calculation, a lookup.</summary>
    Safe,

    /// <summary>Reversible but consequential — a draft saved, a record updated.</summary>
    Sensitive,

    /// <summary>Cannot be taken back — a payment, a message sent, a record deleted.</summary>
    Irreversible
}
