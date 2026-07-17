// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Orchestrations.Agents;

// A record with init-only properties: a nature returns an updated copy via a `with`
// expression and never mutates a shared instance. SPEC.md 3 requires copy-on-write.
public sealed record AgentContext
{
    public string Prompt { get; init; } = "";

    // DATA — what it HAS. Written by Recall.
    public string SystemPrompt { get; init; } = "";
    public IReadOnlyList<string> Observations { get; init; } = [];

    // DECISION — what it THINKS. Written by Think.
    public string Intent { get; init; } = "";
    public string DirectionType { get; init; } = "";
    public string Payload { get; init; } = "";
    public string RawReply { get; init; } = "";

    // DIRECTION — what it DID. Written by Act.
    public string Result { get; init; } = "";
    public AgentStatus Status { get; init; }
}
