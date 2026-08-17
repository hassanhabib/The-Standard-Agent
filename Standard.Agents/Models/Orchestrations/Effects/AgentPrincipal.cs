// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Orchestrations.Effects;

/// <summary>
/// Who is acting, in the terms a policy actually decides on (SPEC.md §4.9).
/// </summary>
/// <remarks>
/// An identifier alone answers "who" and nothing else. Real authorization asks more: a policy
/// engine is routinely written as <i>this principal, in this tenant, under this jurisdiction, on
/// this resource</i> — and a delegated act, where a service is doing something on a person's
/// behalf, is a different question again from that service acting for itself.
/// <para>Every field beyond <see cref="Id"/> is optional. A host that has only a user id supplies
/// only that, and nothing about the perimeter changes; the fields exist so a host that <i>does</i>
/// know more is not forced to smuggle it through a string.</para>
/// <para>The framework <b>consumes</b> a principal, it does not mint one. Establishing identity —
/// tokens, sessions, sign-in — is the host's, and short-lived credentials stay a host concern.</para>
/// </remarks>
public sealed record AgentPrincipal
{
    /// <summary>Who is acting. The one field an identity cannot be without.</summary>
    public string Id { get; init; } = "";

    /// <summary>Which tenant they belong to, where one agent serves several.</summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Which legal or regulatory regime this act falls under — the thing a policy consults when
    /// the same act is permitted in one place and not another.
    /// </summary>
    public string? Jurisdiction { get; init; }

    /// <summary>
    /// Who this principal is acting <i>for</i>, when the act is delegated. A service acting on a
    /// person's behalf is a different act, to a policy, from that service acting for itself.
    /// </summary>
    public string? DelegatedBy { get; init; }

    /// <summary>Reads as the identifier, so a trace or a message says who without ceremony.</summary>
    public override string ToString() =>
        Id;
}
