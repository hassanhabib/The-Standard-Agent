// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Tools;

public interface ITool
{
    string Name { get; }

    string Description => string.Empty;

    string Parameters => "{}";

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
