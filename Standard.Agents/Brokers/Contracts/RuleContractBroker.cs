// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;

namespace Standard.Agents.Brokers.Contracts;

// The Local mode: a validator in the box, so an answer can be held to a shape without the core
// taking a dependency.
//
// It covers the subset a model actually gets wrong — is it JSON at all, is it the right kind of
// thing, are the required members present, and are their types right. It does NOT implement JSON
// Schema: no $ref, no allOf/anyOf/oneOf, no pattern, no numeric bounds, no nested validation past
// the first level.
//
// That boundary is stated rather than discovered, because a partial validator claiming
// completeness is worse than no validator: it would report an answer as conforming to a schema it
// was never checked against. A host that needs the whole specification supplies a real library
// through .UseContract.
public sealed class RuleContractBroker : IContractBroker
{
    public ValueTask<string?> ValidateAsync(string answer, string schema)
    {
        if (string.IsNullOrWhiteSpace(schema))
        {
            return ValueTask.FromResult<string?>(null);
        }

        JsonElement schemaRoot;

        try
        {
            schemaRoot = JsonDocument.Parse(schema).RootElement;
        }
        catch (JsonException)
        {
            // The schema is the host's, not the model's. A broken one is a configuration error and
            // must not be reported as the model's failure, or every revision chases a fault the
            // model cannot fix.
            throw new ArgumentException(
                "The contract schema is not valid JSON.", nameof(schema));
        }

        JsonElement answerRoot;

        try
        {
            answerRoot = JsonDocument.Parse(answer).RootElement;
        }
        catch (JsonException)
        {
            return ValueTask.FromResult<string?>(
                "the answer is not valid JSON; reply with JSON only and no surrounding prose");
        }

        return ValueTask.FromResult(Check(answerRoot, schemaRoot));
    }

    private static string? Check(JsonElement answer, JsonElement schema)
    {
        if (schema.TryGetProperty("type", out JsonElement expectedType)
            && expectedType.ValueKind is JsonValueKind.String
            && Matches(answer.ValueKind, expectedType.GetString()) is false)
        {
            return $"expected {expectedType.GetString()} but the answer was "
                + $"{Describe(answer.ValueKind)}";
        }

        if (answer.ValueKind is not JsonValueKind.Object)
        {
            return null;
        }

        if (schema.TryGetProperty("required", out JsonElement required)
            && required.ValueKind is JsonValueKind.Array)
        {
            foreach (JsonElement member in required.EnumerateArray())
            {
                string? name = member.GetString();

                if (string.IsNullOrEmpty(name) is false
                    && answer.TryGetProperty(name, out _) is false)
                {
                    return $"the required member '{name}' is missing";
                }
            }
        }

        if (schema.TryGetProperty("properties", out JsonElement properties)
            && properties.ValueKind is JsonValueKind.Object)
        {
            foreach (JsonProperty property in properties.EnumerateObject())
            {
                if (answer.TryGetProperty(property.Name, out JsonElement value)
                    && property.Value.TryGetProperty("type", out JsonElement memberType)
                    && memberType.ValueKind is JsonValueKind.String
                    && Matches(value.ValueKind, memberType.GetString()) is false)
                {
                    return $"member '{property.Name}' should be {memberType.GetString()} but was "
                        + $"{Describe(value.ValueKind)}";
                }
            }
        }

        return null;
    }

    // "integer" is deliberately looser than JSON Schema requires: System.Text.Json does not
    // distinguish 1 from 1.0, and rejecting a whole number because it arrived as a double would
    // send the model into a revision it cannot satisfy.
    private static bool Matches(JsonValueKind kind, string? expected) =>
        expected switch
        {
            "object" => kind is JsonValueKind.Object,
            "array" => kind is JsonValueKind.Array,
            "string" => kind is JsonValueKind.String,
            "number" or "integer" => kind is JsonValueKind.Number,
            "boolean" => kind is JsonValueKind.True or JsonValueKind.False,
            "null" => kind is JsonValueKind.Null,
            _ => true,
        };

    private static string Describe(JsonValueKind kind) =>
        kind switch
        {
            JsonValueKind.Object => "an object",
            JsonValueKind.Array => "an array",
            JsonValueKind.String => "a string",
            JsonValueKind.Number => "a number",
            JsonValueKind.True or JsonValueKind.False => "a boolean",
            JsonValueKind.Null => "null",
            _ => "nothing",
        };
}
