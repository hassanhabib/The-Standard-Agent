// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Tools;

public interface ITool
{
    string Name { get; }

    string Description => string.Empty;

    string Parameters => "{}";

    /// <summary>
    /// How consequential this tool is. Declared by the tool, because the tool is what knows —
    /// inferring it from whichever list a host put the name in is how <c>RiskLevel.Sensitive</c>
    /// became a level the framework could never produce.
    /// </summary>
    /// <remarks>
    /// The default is <see cref="RiskLevel.Safe"/>, which is what an unclassified tool was treated
    /// as before. A host can raise it for tools it did not write — an MCP server cannot declare
    /// anything in C# — and the host's word wins, because the host is the one accountable for the
    /// deployment.
    /// </remarks>
    RiskLevel Risk => RiskLevel.Safe;

    /// <summary>
    /// What this tool is about to touch, given its input — a path, a host, an account. Empty when
    /// the tool touches nothing addressable.
    /// </summary>
    /// <remarks>
    /// Permission is not only <i>what</i> but <i>where</i>: "may write files" is not "may write
    /// files under /project". The framework cannot parse arbitrary tool arguments and a host
    /// should not have to reinvent that parsing inside a policy delegate, so the tool — the one
    /// thing that knows what its arguments mean — names the target and the framework carries it to
    /// the policy, the authority and the audit record.
    /// </remarks>
    string ScopeOf(string input) => string.Empty;

    /// <summary>
    /// What the agent says to the user before performing this tool's act — a user-voiced floor
    /// for turns where the model offered no narration of its own; a model-authored narration
    /// takes precedence. Supports <c>{tool}</c> (the tool's name) and <c>{payload}</c> (the
    /// act's input) slots. Empty means no narration, which is exactly today's behavior.
    /// </summary>
    /// <remarks>
    /// Declared by the tool, like <see cref="Risk"/>, because the tool is what knows what its
    /// act means in the user's language. Host-authored text on a framework-known frame, so it is
    /// voiced without a gate call — the only foreign content, the payload, already streamed
    /// verbatim inside the Thinking channel.
    /// </remarks>
    string NarrationStarting => string.Empty;

    /// <summary>
    /// What the agent says to the user after this tool's result has been observed and screened.
    /// Supports the <c>{tool}</c> slot; never overridden by model narration. Empty means no
    /// narration.
    /// </summary>
    string NarrationObserved => string.Empty;

    ValueTask<string> ExecuteAsync(string input);

    /// <summary>
    /// Undoes what <see cref="ExecuteAsync"/> did, given the same input and what it returned.
    /// Return what was done to undo it, or <c>null</c> if this tool cannot be undone.
    /// </summary>
    /// <remarks>
    /// Run-once (SPEC.md §4.9) makes an effect safe to <i>propose</i> twice. Compensation is for
    /// the effects that cannot be made idempotent at all — a payment sent, a message delivered —
    /// where the only way back is a second, opposite act: a refund, a retraction.
    /// <para>Both arguments are needed. The input alone cannot cancel the specific booking that
    /// was made; the outcome carries the identity the undo has to name.</para>
    /// <para>Not every tool can do this, and a tool that cannot says so by leaving the default,
    /// which returns <c>null</c> and is reported as an effect that stands. Silently doing nothing
    /// and reporting success would be the worst of both: the caller believes the run was unwound
    /// when it was not.</para>
    /// </remarks>
    ValueTask<string?> CompensateAsync(string input, string outcome) =>
        ValueTask.FromResult<string?>(null);
}
