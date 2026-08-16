// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Audits;

namespace Standard.Agents.Brokers.Audits;

public interface IAuditBroker
{
    ValueTask WriteAsync(AuditRecord record);
}
