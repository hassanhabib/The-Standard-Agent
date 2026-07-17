// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Orchestrations.Agents;

public sealed record AgentContext
{
    public string Prompt { get; init; } = "";

        public string SystemPrompt { get; init; } = "";
    public IReadOnlyList<string> Observations { get; init; } = [];

        public string Intent { get; init; } = "";
    public string DirectionType { get; init; } = "";
    public string Payload { get; init; } = "";
    public string RawReply { get; init; } = "";

        public string Result { get; init; } = "";
    public AgentStatus Status { get; init; }
}
