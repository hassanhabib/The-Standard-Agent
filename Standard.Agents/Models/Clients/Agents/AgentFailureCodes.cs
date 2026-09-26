// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Clients.Agents;

/// <summary>
/// The codes SPEC.md §3.6 (v1.14) names for a run that stopped without an answer. A host may
/// report codes of its own for stops these do not name, and must not reuse one of these for a
/// different reason.
/// </summary>
public static class AgentFailureCodes
{
    /// <summary>The caller cancelled the run.</summary>
    public const string Cancelled = "cancelled";

    /// <summary>A token, cost or wall-clock budget ran out.</summary>
    public const string BudgetExhausted = "budget_exhausted";

    /// <summary>The run reached its turn cap still working.</summary>
    public const string TurnsExhausted = "turns_exhausted";

    /// <summary>The run kept asking for an act the ledger had already answered.</summary>
    public const string GoingInCircles = "going_in_circles";
}
