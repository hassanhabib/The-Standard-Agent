// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Standard.Agents;

// The agent document, described once. This table is what FromJson validates a section's shape
// against (Configurations.cs reads it for every object section) and what the JSON Schema is
// emitted from, so an editor's completion list and the builder's refusals cannot disagree:
// there is one list (principal review 2026-09-04, F-24).
public sealed partial class StandardAgent
{
    private const string StringKind = "string";
    private const string NumberKind = "number";
    private const string IntegerKind = "integer";
    private const string BooleanKind = "boolean";
    private const string ObjectKind = "object";
    private const string ArrayKind = "array";

    // One property of an object section: its JSON kind, whether the section needs it, and
    // whether it must be positive - the same rules the switch enforces.
    private sealed record DocumentProperty(
        string Name,
        string Kind,
        bool Required = false,
        bool Positive = false,
        string[]? Names = null);

    // One top-level key: the shapes it accepts, the properties of its object shape, the kind of
    // its array items, the names of its enum shape, and an example that composes.
    private sealed record DocumentSection(
        string Key,
        string Description,
        string[] Shapes,
        string Example,
        DocumentProperty[]? Properties = null,
        string? ItemsKind = null,
        string? ItemsSection = null,
        string[]? Names = null,
        bool BooleanMustBeFalse = false);

    private static readonly DocumentProperty[] endpointProperties =
    [
        new("apiUrl", StringKind, Required: true),
        new("apiKey", StringKind),
        new("model", StringKind, Required: true),
        new("temperature", NumberKind),
        new("maxTokens", IntegerKind, Positive: true),
        new("timeoutSeconds", IntegerKind, Positive: true)
    ];

    private static readonly DocumentSection[] documentSections =
    [
        new("name", "The agent's name; a handoff calls agents by it.", [StringKind], "\"refund-desk\""),
        new("description", "What the agent is for, as a registry advertises it.", [StringKind], "\"Handles refunds and invoices.\""),
        new("brain", "The text-protocol brain on an OpenAI-compatible endpoint.", [ObjectKind],
            "{ \"apiUrl\": \"http://localhost:11434/v1/\", \"model\": \"LLooMA2.0\" }",
            Properties: endpointProperties),
        new("nativeBrain", "A native tool-calling brain on an OpenAI-compatible endpoint.", [ObjectKind],
            "{ \"apiUrl\": \"http://localhost:11434/v1/\", \"model\": \"LLooMA2.0\" }",
            Properties: [.. endpointProperties.Where(property => property.Name != "timeoutSeconds")]),
        new("nativeBrainAnthropic", "A native tool-calling brain on the Anthropic Messages API.", [ObjectKind],
            "{ \"apiKey\": \"k\", \"model\": \"claude-sonnet-5\" }",
            Properties:
            [
                new("apiKey", StringKind, Required: true),
                new("model", StringKind, Required: true),
                new("temperature", NumberKind),
                new("maxTokens", IntegerKind, Positive: true)
            ]),
        new("skills", "A skills folder, or several.", [StringKind, ArrayKind], "\"Skills\"", ItemsKind: StringKind),
        new("knowledge", "A knowledge folder, or the folder with its pattern and ranking.", [StringKind, ObjectKind],
            "\"Knowledge\"",
            Properties:
            [
                new("path", StringKind, Required: true),
                new("pattern", StringKind),
                new("maxResults", IntegerKind, Positive: true),
                new("minScore", NumberKind)
            ]),
        new("memory", "A memory file, or false to run without one.", [StringKind, BooleanKind], "\"memory.txt\"",
            BooleanMustBeFalse: true),
        new("mcp", "An MCP server by URL, or with its auth, or several.", [StringKind, ObjectKind, ArrayKind],
            "\"http://localhost:8080/mcp/\"",
            Properties:
            [
                new("endpointUrl", StringKind, Required: true),
                new("relativeUrl", StringKind),
                new("timeoutSeconds", IntegerKind, Positive: true),
                new("bearerToken", StringKind),
                new("apiKey", StringKind),
                new("apiKeyHeader", StringKind)
            ],
            ItemsSection: "mcp"),
        new("agents", "A folder of agent documents, or inline members, for handoffs.", [StringKind, ArrayKind],
            "\"Agents\"",
            ItemsSection: "agents"),
        new("gate", "The guardian that screens the request, on an endpoint.", [ObjectKind],
            "{ \"apiUrl\": \"http://localhost:11434/v1/\", \"model\": \"LLooMA2.0\" }",
            Properties: endpointProperties),
        new("ruleGate", "Patterns the gate refuses without a model.", [ArrayKind], "[\"password\"]", ItemsKind: StringKind),
        new("judge", "The guardian that reviews the answer, on an endpoint.", [ObjectKind],
            "{ \"apiUrl\": \"http://localhost:11434/v1/\", \"model\": \"LLooMA2.0\" }",
            Properties: endpointProperties),
        new("ruleJudge", "Patterns the judge rejects without a model.", [ArrayKind], "[\"as an AI\"]", ItemsKind: StringKind),
        new("contract", "The JSON Schema the answer must satisfy, embedded.", [ObjectKind], "{ \"type\": \"object\" }"),
        new("constitution", "The law above both guardians, a Markdown file.", [StringKind], "\"Constitution/ethics.md\""),
        new("consumption", "The consumption prompt, a Markdown file.", [StringKind], "\"Consumption/rules.md\""),
        new("redact", "PII redaction at the brain boundary: true for the default rules, or your own.",
            [BooleanKind, ObjectKind], "true",
            Properties: [new("rules", ArrayKind, Required: true)]),
        new("maxTurns", "How many turns a run may take.", [IntegerKind], "7"),
        new("allowTools", "The only tools the agent may call; empty closes the perimeter.", [ArrayKind],
            "[\"calculator\"]", ItemsKind: StringKind),
        new("permissions", "The disposition toward acts nothing names.", [StringKind], "\"Ask\"",
            Names: ["Open", "Ask", "Deny"]),
        new("risk", "Which tools carry which risk level.", [ObjectKind], "{ \"Irreversible\": [\"wire_transfer\"] }",
            Properties:
            [
                new("Safe", ArrayKind),
                new("Sensitive", ArrayKind),
                new("Irreversible", ArrayKind)
            ]),
        new("requireApproval", "Tools a person must approve before they run.", [ArrayKind], "[\"wire_transfer\"]",
            ItemsKind: StringKind),
        new("logTo", "The human-readable trace, a file, or the file with its verbosity.", [StringKind, ObjectKind],
            "\"log.txt\"",
            Properties:
            [
                new("path", StringKind, Required: true),
                new("verbosity", StringKind, Names: ["Summary", "Natures", "Full"])
            ]),
        new("audit", "The decision log, JSON lines.", [StringKind], "\"audit.jsonl\""),
        new("auditPayloads", "Record payloads in the decision log, as redaction leaves them.", [BooleanKind], "true"),
        new("telemetry", "OpenTelemetry spans and metrics, under this service name, or true.", [StringKind, BooleanKind],
            "\"teller-agent\""),
        new("sessions", "Conversations kept in a folder, or the folder with its history bound.", [StringKind, ObjectKind],
            "\"Sessions\"",
            Properties:
            [
                new("path", StringKind, Required: true),
                new("maxHistoryTurns", IntegerKind, Positive: true)
            ]),
        new("effectLedger", "The run-once ledger folder.", [StringKind], "\"ledger\""),
        new("effectLeaseSeconds", "How long an in-flight claim is presumed live.", [NumberKind], "300"),
        new("screenToolOutput", "Screen tool results with the gate before the brain sees them.", [BooleanKind], "true"),
        new("budget", "Bounds on tokens, cost and wall clock.", [ObjectKind], "{ \"maxTokens\": 50000 }",
            Properties:
            [
                new("maxTokens", IntegerKind, Positive: true),
                new("maxCostUsd", NumberKind, Positive: true),
                new("maxWallClockSeconds", NumberKind, Positive: true),
                new("costPerThousandTokens", NumberKind)
            ]),
        new("usage", "How tokens are counted when the provider does not say.", [ObjectKind],
            "{ \"charactersPerToken\": 4 }",
            Properties: [new("charactersPerToken", NumberKind, Positive: true)]),
        new("resilience", "Retries on the brain, as a count or an object.", [IntegerKind, ObjectKind], "3",
            Properties: [new("retries", IntegerKind)]),
        new("compensateOnFailure", "Unwind performed effects when the run fails.", [BooleanKind], "true")
    ];

    // Nested object sections, validated by the same table under a dotted key.
    private static readonly DocumentSection[] nestedSections =
    [
        new("redact.rules", "One redaction rule: a label and a pattern.", [ObjectKind],
            "{ \"label\": \"CARD\", \"pattern\": \"\\\\d{4}-\\\\d{4}-\\\\d{4}-\\\\d{4}\" }",
            Properties:
            [
                new("label", StringKind, Required: true),
                new("pattern", StringKind, Required: true)
            ])
    ];

    private static string[] DocumentPropertyNames(string key) =>
        [.. documentSections.Concat(nestedSections)
            .Single(section => section.Key == key)
            .Properties!.Select(property => property.Name)];

    /// <summary>
    /// The JSON Schema of the agent document <see cref="FromJson"/> composes, for editors and
    /// pipelines. Emitted from the same table the document is validated against, so what an
    /// editor offers and what the builder refuses cannot disagree.
    /// </summary>
    /// <returns>A JSON Schema (draft 2020-12) as text.</returns>
    public static string DocumentSchemaJson()
    {
        var properties = new JsonObject();

        foreach (DocumentSection section in documentSections)
        {
            properties[section.Key] = DescribeSection(section);
        }

        var schema = new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["title"] = "Standard.Agents agent document",
            ["description"] = "The whole configurable surface of a StandardAgent as data: one key per builder verb.",
            ["type"] = ObjectKind,
            ["additionalProperties"] = false,
            ["properties"] = properties
        };

        return schema.ToJsonString(schemaOptions);
    }

    // Indented for a person, and with plain quotes: an editor reads this file, not a browser,
    // so nothing here needs HTML-safe escaping. Derived from the defaults so net8.0's serializer
    // has its type resolver.
    private static readonly JsonSerializerOptions schemaOptions =
        new(JsonSerializerOptions.Default)
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

    private static JsonObject DescribeSection(DocumentSection section)
    {
        var shapes = new JsonArray();

        foreach (string shape in section.Shapes)
        {
            shapes.Add(DescribeShape(section, shape));
        }

        return new JsonObject
        {
            ["description"] = section.Description,
            ["anyOf"] = shapes,
            ["examples"] = new JsonArray(JsonNode.Parse(section.Example))
        };
    }

    private static JsonObject DescribeShape(DocumentSection section, string shape) =>
        shape switch
        {
            ObjectKind => DescribeObject(section),
            ArrayKind => DescribeArray(section),
            StringKind when section.Names is not null =>
                new JsonObject { ["type"] = StringKind, ["enum"] = new JsonArray([.. section.Names.Select(name => (JsonNode)name)]) },
            BooleanKind when section.BooleanMustBeFalse =>
                new JsonObject { ["type"] = BooleanKind, ["const"] = false },
            _ => new JsonObject { ["type"] = shape }
        };

    // A section with no property list (the contract, which is a JSON Schema itself) stays open;
    // every other object is closed, exactly as Shaped closes it.
    private static JsonObject DescribeObject(DocumentSection section)
    {
        if (section.Properties is null)
        {
            return new JsonObject { ["type"] = ObjectKind };
        }

        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (DocumentProperty property in section.Properties)
        {
            properties[property.Name] = DescribeProperty(property);

            if (property.Required)
            {
                required.Add(property.Name);
            }
        }

        var described = new JsonObject
        {
            ["type"] = ObjectKind,
            ["additionalProperties"] = false,
            ["properties"] = properties
        };

        if (required.Count > 0)
        {
            described["required"] = required;
        }

        return described;
    }

    private static JsonObject DescribeProperty(DocumentProperty property)
    {
        var described = new JsonObject { ["type"] = property.Kind };

        if (property.Positive)
        {
            described["exclusiveMinimum"] = 0;
        }

        if (property.Names is not null)
        {
            described["enum"] = new JsonArray([.. property.Names.Select(name => (JsonNode)name)]);
        }

        if (property.Kind == ArrayKind)
        {
            described["items"] = property.Name == "rules"
                ? DescribeObject(nestedSections.Single(nested => nested.Key == "redact.rules"))
                : new JsonObject { ["type"] = StringKind };
        }

        return described;
    }

    // An array's items are strings, or the section's own object shape beside a string (an MCP
    // server by URL or by object; a fleet member by path or as an inline document).
    private static JsonObject DescribeArray(DocumentSection section)
    {
        JsonObject items = section.ItemsSection is null
            ? new JsonObject { ["type"] = section.ItemsKind ?? StringKind }
            : new JsonObject
            {
                ["anyOf"] = new JsonArray(
                    new JsonObject { ["type"] = StringKind },
                    section.Properties is null
                        ? new JsonObject { ["type"] = ObjectKind }
                        : DescribeObject(section))
            };

        return new JsonObject
        {
            ["type"] = ArrayKind,
            ["minItems"] = 1,
            ["items"] = items
        };
    }
}
