// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Generators.V1;

namespace Standard.Agents.Models.Brokers.Generators;

/// <summary>
/// The output of precedence — configured → request → framework default, resolved once at the
/// boundary (docs/per-request-inference.md §2, §4). This is what rides the context and reaches
/// the brokers; <c>PromptRequest</c> never travels below the entry, so a tier handed this record
/// cannot see a value precedence discarded. That is what makes the dangerous state — a model
/// constrained to one schema and validated against another — unreachable rather than avoided.
/// </summary>
public sealed record ResolvedInference
{
    // The framework's third rung, applied by resolution at the boundary and nowhere else.
    // Brokers receive concrete values and decide nothing.
    public const double DefaultTemperature = 0.7;
    public const int DefaultMaxTokens = 1024;

    // Concrete, not nullable: precedence has already run, so the framework default has already
    // been applied. No tier below the entry ever supplies a default.
    public double Temperature { get; init; } = DefaultTemperature;
    public int MaxTokens { get; init; } = DefaultMaxTokens;

    // Nullable where "absent from the wire" is itself the meaning.
    public int? Seed { get; init; }
    public IReadOnlyList<string> Stop { get; init; } = [];

    /// <summary>
    /// The schema that SURVIVED precedence — configured when a Contract exists, the request's
    /// otherwise. There is no field for the losing schema.
    /// </summary>
    public string? ResponseSchemaJson { get; init; }

    /// <summary>
    /// The caller's tools — vocabulary for the model, never capability for the agent. Direction
    /// classifies a call naming one as a terminal answer addressed to the caller.
    /// </summary>
    public IReadOnlyList<ToolDefinition> CallerTools { get; init; } = [];

    // Opaque to the core; the broker merges it under the core-owned-keys rule (§4.4): every key
    // the core writes is non-overridable, stripped on collision and logged.
    public string? ProviderOptionsJson { get; init; }
}
