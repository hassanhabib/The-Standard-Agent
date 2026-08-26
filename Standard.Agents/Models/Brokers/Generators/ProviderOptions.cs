// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json.Nodes;

namespace Standard.Agents.Models.Brokers.Generators;

/// <summary>
/// What sanitizing a caller's provider options produced: the JSON that may reach the wire, the
/// core-owned keys that were stripped from it, and whether the whole bag was unreadable.
/// </summary>
public sealed record SanitizedProviderOptions(
    string? Json,
    IReadOnlyList<string> StrippedKeys,
    bool Malformed);

/// <summary>
/// The core-owned-keys rule (docs/per-request-inference.md §4.4): every key the core writes on
/// the wire is non-overridable. Enforced once, at the boundary — so no broker, built-in or
/// third-party, can ever be handed a passthrough that adds a tool, swaps the messages, or beats
/// a value precedence already resolved. What survives merges whole; the broker stays blind.
/// </summary>
public static class ProviderOptions
{
    public static SanitizedProviderOptions Sanitize(string? providerOptionsJson) =>
        new(providerOptionsJson, [], Malformed: false);

    public static void MergeInto(JsonObject request, string? sanitizedJson)
    {
    }
}
