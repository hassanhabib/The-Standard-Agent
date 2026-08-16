// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Audits;

namespace Standard.Agents.Brokers.Audits;

// The Custom mode's substrate: a host-supplied function standing in for a broker, the same
// shape FunctionGeneratorBroker gives a host-supplied brain.
public sealed class FunctionAuditBroker : IAuditBroker
{
    private readonly Func<AuditRecord, ValueTask> write;

    public FunctionAuditBroker(Func<AuditRecord, ValueTask> write) =>
        this.write = write;

    public ValueTask WriteAsync(AuditRecord record) =>
        this.write(record);
}
