// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Foundations.Usages;

/// <summary>
/// What one model call consumed.
/// </summary>
/// <param name="PromptTokens">Tokens sent — system prompt, observations and the user message.</param>
/// <param name="CompletionTokens">Tokens the model produced in reply.</param>
/// <param name="IsEstimated">
/// <c>false</c> when the provider reported these numbers itself, <c>true</c> when they were
/// counted locally. A budget can be enforced on either, but only a reported number can be
/// reconciled against an invoice — so the distinction travels with the number rather than being
/// inferred from which brain happened to be configured.
/// </param>
public record AgentUsage(
    int PromptTokens,
    int CompletionTokens,
    bool IsEstimated)
{
    /// <summary>A call that consumed nothing — the identity for accumulation.</summary>
    public static AgentUsage None { get; } =
        new AgentUsage(PromptTokens: 0, CompletionTokens: 0, IsEstimated: false);

    public int TotalTokens => PromptTokens + CompletionTokens;
}
