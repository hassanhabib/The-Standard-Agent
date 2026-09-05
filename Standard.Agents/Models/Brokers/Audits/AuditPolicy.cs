// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Brokers.Audits;

/// <summary>
/// What the decision log may carry. <see cref="Payloads"/> false, the default, records metadata:
/// the event, the actor, the time, the principal, and the length and hash of any payload, never
/// the payload itself. True records the payloads too, as the configured redaction leaves them.
/// A deliberate opt-in, because an audit sink usually has broader access and a longer life than
/// anything at runtime (principal review 2026-09-04, F-14).
/// </summary>
public sealed record AuditPolicy(bool Payloads)
{
    public static AuditPolicy Metadata => new(Payloads: false);
}
