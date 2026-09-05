// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Coordinations.Agents;

/// <summary>
/// What one prompt is allowed to consume (SPEC.md §4.10). Any bound left null is unbounded, so
/// a budget constrains exactly what the host chose to constrain and nothing else.
/// </summary>
public sealed record AgentBudget
{
    public int? MaxTokens { get; init; }
    public decimal? MaxCostUsd { get; init; }
    public TimeSpan? MaxWallClock { get; init; }

    /// <summary>
    /// Cost per 1,000 tokens, when the host wants a cost bound rather than a token one. The
    /// builder requires it to be positive whenever <see cref="MaxCostUsd"/> is set: spend is
    /// the token count times this rate, so a zero rate is a cost bound that can never trip.
    /// </summary>
    public decimal CostPerThousandTokens { get; init; }

    public bool IsBounded =>
        MaxTokens is not null || MaxCostUsd is not null || MaxWallClock is not null;
}
