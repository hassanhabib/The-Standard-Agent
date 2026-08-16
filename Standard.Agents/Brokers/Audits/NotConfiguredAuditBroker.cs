// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Audits;

namespace Standard.Agents.Brokers.Audits;

// The default. SPEC.md §4.7: absent configuration no decision log is emitted, no
// sink is touched, and behavior is exactly as if the section did not exist.
public sealed class NotConfiguredAuditBroker : IAuditBroker
{
    public ValueTask WriteAsync(AuditRecord record) =>
        ValueTask.CompletedTask;
}
