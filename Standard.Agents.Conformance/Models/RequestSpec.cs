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
public sealed record TurnSpec(string Prompt, string Answer);

/// <summary>
/// A tool the CALLER will execute, declared so the model may name it. The agent never runs one
/// (design §6): a call naming it is a terminal answer addressed to the caller.
/// </summary>
public sealed record CallerToolSpec(
    string Name,
    string Description = "",
    string ParametersJson = "{}");
