// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Brokers.Audits;

public sealed record AuditRecord
{
    public string RunId { get; init; } = "";
    public int Sequence { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public AuditKind Kind { get; init; }

    public string Actor { get; init; } = "";
    public string Message { get; init; } = "";
    public bool Detail { get; init; }

    public string? Principal { get; init; }
    public string? PreviousHash { get; init; }
}
