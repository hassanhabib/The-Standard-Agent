// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models;

public sealed record AgentContext
{
    public string Prompt { get; init; } = string.Empty;

    public string SystemPrompt { get; init; } = string.Empty;
    public IReadOnlyList<string> Observations { get; init; } = [];

    public string Intent { get; init; } = string.Empty;
    public string DirectionType { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public string RawReply { get; init; } = string.Empty;

    public string Result { get; init; } = string.Empty;
    public AgentStatus Status { get; init; }
}
