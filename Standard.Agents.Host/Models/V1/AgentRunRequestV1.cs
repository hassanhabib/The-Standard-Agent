// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Host.Models.V1;

/// <summary>
/// What to run, in full: the wire form of <c>PromptRequest</c>, version 1 of the host's
/// contract. Everything about a run that is data travels here — the session to continue,
/// the caller-owned transcript, the tool calls the caller already executed, the tools the
/// caller will execute, and the caller's inference controls. A hosted agent that composed
/// sessions, approvals and caller tools could not use any of them over HTTP when the wire
/// carried a prompt alone (principal review 2026-09-04, F-10).
/// </summary>
/// <remarks>
/// Deliberately absent, as on <c>PromptRequest</c>: executable tools, permissions, budget,
/// redaction, approvals, principal. The wire has no field in which to ask for them.
/// <para>Continuing a held run is this same request: post the authority's decision, or the
/// person's reply, as the prompt on the same session. Completing a caller's tool call is this
/// same request too: post the exchange, naming the call id the model minted, on the same
/// session.</para>
/// </remarks>
public sealed record AgentRunRequestV1
{
    public string Prompt { get; init; } = "";
    public string SessionId { get; init; } = "";
    public IReadOnlyList<AgentTurnV1> History { get; init; } = [];
    public IReadOnlyList<ToolExchangeV1> ToolExchanges { get; init; } = [];
    public IReadOnlyList<CallerToolV1> CallerTools { get; init; } = [];
    public string? ResponseSchemaJson { get; init; }
    public double? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public int? Seed { get; init; }
    public IReadOnlyList<string> Stop { get; init; } = [];
    public string? ProviderOptionsJson { get; init; }
}
