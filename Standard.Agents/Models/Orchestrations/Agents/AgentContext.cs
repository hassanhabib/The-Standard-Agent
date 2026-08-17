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
    public string Route { get; init; } = "";
    public string DirectionType { get; init; } = "";
    public string Payload { get; init; } = "";
    public string RawReply { get; init; } = "";

    // What the turn's model calls actually cost, as reported by the provider (SPEC.md §3.4).
    // Zero when a provider reports nothing, which is why a budget bounds reported usage rather
    // than an estimate — an implementation must not present an estimate as a measurement.
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }

    public string Result { get; init; } = "";
    public string Remember { get; init; } = "";
    public AgentStatus Status { get; init; }
}
