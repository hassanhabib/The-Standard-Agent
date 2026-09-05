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

    /// <summary>
    /// The payload behind a process record (the prompt, the system prompt, the reply, a tool's
    /// input or output), present only when the deployment opted into payload capture and then
    /// as the redaction left it. Withheld by default: the decision log records that a payload
    /// existed, how large it was and which one it was (<see cref="PayloadLength"/>,
    /// <see cref="PayloadHash"/>), never the payload itself (principal review 2026-09-04, F-14).
    /// </summary>
    public string? Payload { get; init; }
    public int? PayloadLength { get; init; }
    public string? PayloadHash { get; init; }
    public string? PreviousHash { get; init; }
}
