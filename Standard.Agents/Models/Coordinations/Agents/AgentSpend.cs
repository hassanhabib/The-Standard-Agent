// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Coordinations.Agents;

/// <summary>
/// What one run has consumed so far. Tokens accumulate from what providers actually reported —
/// SPEC.md §3.4 forbids presenting an estimate as a measurement, so an unreported call adds
/// nothing here rather than adding a guess.
/// </summary>
public sealed class AgentSpend
{
    private int tokens;

    public int Tokens => Volatile.Read(ref this.tokens);

    public void AddTokens(int promptTokens, int completionTokens) =>
        Interlocked.Add(ref this.tokens, promptTokens + completionTokens);

    public decimal CostUsd(decimal costPerThousandTokens) =>
        Tokens / 1000m * costPerThousandTokens;
}
