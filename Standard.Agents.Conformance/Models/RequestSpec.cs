// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Conformance;

/// <summary>
/// What one caller asked for, per request (docs/per-request-inference.md §3). Every field is
/// optional because "unset" must be representable — precedence depends on distinguishing a
/// value the caller chose from one they never mentioned.
/// </summary>
public sealed record RequestSpec(
    double? Temperature = null,
    int? MaxTokens = null,
    int? Seed = null,
    List<string>? Stop = null,
    string? ResponseSchemaJson = null,
    string? ProviderOptionsJson = null,
    List<CallerToolSpec>? CallerTools = null,

    // The caller-owned transcript (design §3): the exposed protocols are stateless and the
    // client re-posts the conversation. When a session exists it wins.
    List<TurnSpec>? History = null);

/// <summary>One prior exchange in the caller-owned transcript, oldest first.</summary>
/// <remarks>
/// A turn carries what it DID as well as what it said. Without <see cref="Exchanges"/> a vector
/// could only say that a prior turn's text reached the Brain, which is all vector 63 ever proved;
/// a caller re-posting a conversation had nowhere to put a finished call but the request's own
/// <c>toolExchanges</c>, this turn's in-flight work, where it certifies as evidence for the
/// prompt being asked now (SPEC.md 4.11, SPEC.md 6).
/// </remarks>
public sealed record TurnSpec(
    string Prompt,
    string Answer,
    List<ToolExchangeSpec>? Exchanges = null);

/// <summary>One call a prior turn made, and what it returned.</summary>
public sealed record ToolExchangeSpec(
    string CallId,
    string ToolName,
    string ArgumentsJson,
    string Result);

/// <summary>
/// A tool the CALLER will execute, declared so the model may name it. The agent never runs one
/// (design §6): a call naming it is a terminal answer addressed to the caller.
/// </summary>
public sealed record CallerToolSpec(
    string Name,
    string Description = "",
    string ParametersJson = "{}");
