// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
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
/// the wire is non-overridable. Enforced at the boundary by <see cref="Sanitize"/> — so no
/// broker, built-in or third-party, can ever be handed a passthrough that adds a tool, swaps the
/// messages, or beats a value precedence already resolved — and again by <see cref="MergeInto"/>,
/// because this is the perimeter and one enforcement point is one forgotten call away from none.
/// </summary>
public static class ProviderOptions
{
    // Everything the core writes on the wire, on either protocol. The modeled field wins
    // because the modeled field is the one precedence was applied to; a raw key that could beat
    // it would be a second resolution path with no ceiling.
    private static readonly HashSet<string> coreOwnedKeys = new(StringComparer.Ordinal)
    {
        "model",
        "messages",
        "tools",
        "response_format",
        "temperature",
        "max_tokens",
        "seed",
        "stop",
        "stream"
    };

    public static SanitizedProviderOptions Sanitize(string? providerOptionsJson)
    {
        if (string.IsNullOrWhiteSpace(providerOptionsJson))
        {
            return new SanitizedProviderOptions(Json: null, StrippedKeys: [], Malformed: false);
        }

        JsonObject options;

        try
        {
            if (JsonNode.Parse(providerOptionsJson) is not JsonObject parsed)
            {
                return new SanitizedProviderOptions(
                    Json: null, StrippedKeys: [], Malformed: true);
            }

            options = parsed;
        }
        catch (JsonException)
        {
            return new SanitizedProviderOptions(Json: null, StrippedKeys: [], Malformed: true);
        }

        string[] stripped =
            [.. options.Select(pair => pair.Key).Where(coreOwnedKeys.Contains)];

        foreach (string key in stripped)
        {
            options.Remove(key);
        }

        return new SanitizedProviderOptions(
            Json: options.Count > 0 ? options.ToJsonString() : null,
            StrippedKeys: stripped,
            Malformed: false);
    }

    public static void MergeInto(JsonObject request, string? sanitizedJson)
    {
        SanitizedProviderOptions sanitized = Sanitize(sanitizedJson);

        if (sanitized.Json is null)
        {
            return;
        }

        JsonObject options = JsonNode.Parse(sanitized.Json)!.AsObject();

        // Whole values, detached before they land: a JsonNode belongs to one tree at a time.
        foreach (string key in options.Select(pair => pair.Key).ToArray())
        {
            JsonNode? value = options[key];
            options.Remove(key);
            request[key] = value;
        }
    }
}
