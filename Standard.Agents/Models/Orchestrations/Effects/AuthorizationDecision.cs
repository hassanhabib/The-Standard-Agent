// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Orchestrations.Effects;

/// <summary>
/// Whether an effect may proceed, and why. The reason is not optional: a denial without one
/// cannot be audited, cannot be appealed, and cannot be told apart from a fault (SPEC.md §4.1).
/// </summary>
public sealed record AuthorizationDecision(bool Permitted, string Reason)
{
    public static AuthorizationDecision Allow() =>
        new(Permitted: true, Reason: string.Empty);

    public static AuthorizationDecision Deny(string reason) =>
        new(Permitted: false, Reason: reason);
}
