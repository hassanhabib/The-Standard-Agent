// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;
using Standard.Agents.Models.Clients.Agents.Exceptions;
using Standard.Agents.Models.Foundations.Brains;
using Standard.Agents.Models.Loggings;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents;

public partial class StandardAgent
{
    /// <summary>
    /// Composes an agent from a JSON document — the whole configurable surface as data, one key
    /// per capability, the same names as the builder verbs. Anything about an agent that is data
    /// can arrive as data: a low-code form, a database row, a request body some platform stored.
    /// Tools stay code because they are code — except MCP, where a tool is a URL, which is data.
    /// An unknown key throws with the key named rather than composing an agent that silently
    /// lacks a control the caller believes is on.
    /// </summary>
    /// <param name="json">The configuration document.</param>
    /// <returns>The composed agent — keep chaining code (tools, delegates) after it.</returns>
    public static StandardAgent FromJson(string json)
    {
        JsonObject document = ParseDocument(json);
        var agent = new StandardAgent();

        foreach ((string key, JsonNode? value) in document)
        {
            Apply(agent, key, value);
        }

        return agent;
    }

    /// <summary>Reads <paramref name="path"/> and composes the agent from its JSON.</summary>
    /// <param name="path">Path to the configuration document (e.g. <c>agent.json</c>).</param>
    /// <returns>The composed agent — keep chaining code (tools, delegates) after it.</returns>
    public static StandardAgent FromJsonFile(string path) =>
        FromJson(File.ReadAllText(path));

    private static JsonObject ParseDocument(string json)
    {
        JsonNode? parsed;

        try
        {
            parsed = JsonNode.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new InvalidAgentConfigurationException(
                $"The configuration is not valid JSON: {exception.Message}");
        }

        return parsed as JsonObject
            ?? throw new InvalidAgentConfigurationException(
                "The configuration must be a JSON object with one key per capability.");
    }

    // One case per capability, named exactly as the builder verb it drives — the parity test
    // feeds a document containing every key, so a verb added without a binding turns red there.
    private static void Apply(StandardAgent agent, string key, JsonNode? value)
    {
        switch (key)
        {
            case "brain":
                agent.Brain(
                    Text(value, key, "apiUrl"),
                    Text(value, key, "apiKey", fallback: string.Empty),
                    Text(value, key, "model"),
                    Number(value, "temperature", fallback: 0.7),
                    Whole(value, "maxTokens", fallback: 1024),
                    Whole(value, "timeoutSeconds", fallback: 120));

                break;

            case "nativeBrain":
                agent.NativeBrain(
                    Text(value, key, "apiUrl"),
                    Text(value, key, "apiKey", fallback: string.Empty),
                    Text(value, key, "model"),
                    Number(value, "temperature", fallback: 0.7),
                    Whole(value, "maxTokens", fallback: 1024));

                break;

            case "nativeBrainAnthropic":
                agent.NativeBrainAnthropic(
                    Text(value, key, "apiKey"),
                    Text(value, key, "model"),
                    Number(value, "temperature", fallback: 0.7),
                    Whole(value, "maxTokens", fallback: 1024));

                break;

            case "skills" when value is JsonArray sources:
                foreach (JsonNode? source in sources)
                {
                    agent.Skills(Text(source, key));
                }

                break;

            case "skills":
                agent.Skills(Text(value, key));

                break;

            case "knowledge" when value is JsonObject:
                agent.Knowledge(
                    Text(value, key, "path"),
                    Text(value, key, "pattern", fallback: "*.md"),
                    Whole(value, "maxResults", fallback: 3),
                    Number(value, "minScore", fallback: 0.0));

                break;

            case "knowledge":
                agent.Knowledge(Text(value, key));

                break;

            case "memory":
                agent.Memory(Text(value, key));

                break;

            case "mcp" when value is JsonArray servers:
                foreach (JsonNode? server in servers)
                {
                    ApplyMcpServer(agent, server, key);
                }

                break;

            case "mcp":
                ApplyMcpServer(agent, value, key);

                break;

            case "gate":
                agent.Gate(
                    Text(value, key, "apiUrl"),
                    Text(value, key, "apiKey", fallback: string.Empty),
                    Text(value, key, "model"),
                    Number(value, "temperature", fallback: 0.0),
                    Whole(value, "maxTokens", fallback: 16),
                    Whole(value, "timeoutSeconds", fallback: 30));

                break;

            case "ruleGate":
                agent.RuleGate(Texts(value, key));

                break;

            case "judge":
                agent.Judge(
                    Text(value, key, "apiUrl"),
                    Text(value, key, "apiKey", fallback: string.Empty),
                    Text(value, key, "model"),
                    Number(value, "temperature", fallback: 0.0),
                    Whole(value, "maxTokens", fallback: 16),
                    Whole(value, "timeoutSeconds", fallback: 30));

                break;

            case "ruleJudge":
                agent.RuleJudge(Texts(value, key));

                break;

            // The contract is itself a JSON schema, so it rides embedded rather than as an
            // escaped string a form author would have to hand-quote.
            case "contract":
                agent.Contract(value?.ToJsonString() ?? "{}");

                break;

            case "constitution":
                agent.Constitution(Text(value, key));

                break;

            case "consumption":
                agent.Consumption(Text(value, key));

                break;

            case "redact" when value is JsonObject rules:
                agent.Redact([.. Items(rules["rules"], "redact.rules").Select(rule =>
                    new RedactionRule
                    {
                        Label = Text(rule, "redact.rules", "label"),
                        Pattern = Text(rule, "redact.rules", "pattern")
                    })]);

                break;

            case "redact" when Truth(value, key):
                agent.Redact();

                break;

            case "redact":
                break;

            case "maxTurns":
                agent.MaxTurns(Whole(value, key));

                break;

            case "allowTools":
                agent.AllowTools(Texts(value, key));

                break;

            case "permissions":
                agent.Permissions(Named<PermissionMode>(value, key));

                break;

            case "risk":
                foreach ((string levelName, JsonNode? toolNames) in Entries(value, key))
                {
                    agent.Risk(NamedText<RiskLevel>(levelName, key), Texts(toolNames, key));
                }

                break;

            case "requireApproval":
                agent.RequireApproval(Texts(value, key));

                break;

            case "logTo" when value is JsonObject:
                agent.LogTo(
                    Text(value, key, "path"),
                    NamedText<TraceVerbosity>(
                        Text(value, key, "verbosity", fallback: "Full"), key));

                break;

            case "logTo":
                agent.LogTo(Text(value, key));

                break;

            case "audit":
                agent.Audit(Text(value, key));

                break;

            case "telemetry" when value?.GetValueKind() is JsonValueKind.String:
                agent.Telemetry(Text(value, key));

                break;

            case "telemetry" when Truth(value, key):
                agent.Telemetry();

                break;

            case "telemetry":
                break;

            case "sessions" when value is JsonObject:
                agent.Sessions(
                    Text(value, key, "path"),
                    Whole(value, "maxHistoryTurns", fallback: 20));

                break;

            case "sessions":
                agent.Sessions(Text(value, key));

                break;

            case "effectLedger":
                agent.EffectLedger(Text(value, key));

                break;

            case "screenToolOutput" when Truth(value, key):
                agent.ScreenToolOutput();

                break;

            case "screenToolOutput":
                break;

            case "budget":
                agent.Budget(
                    OptionalWhole(value, "maxTokens"),
                    OptionalMoney(value, "maxCostUsd"),
                    OptionalSeconds(value, "maxWallClockSeconds"),
                    OptionalMoney(value, "costPerThousandTokens") ?? 0m);

                break;

            case "usage":
                agent.Usage(Number(value, "charactersPerToken", fallback: 4.0));

                break;

            case "resilience" when value is JsonObject:
                agent.Resilience(Whole(value, "retries", fallback: 3));

                break;

            case "resilience":
                agent.Resilience(Whole(value, key));

                break;

            case "compensateOnFailure" when Truth(value, key):
                agent.CompensateOnFailure();

                break;

            case "compensateOnFailure":
                break;

            default:
                throw new InvalidAgentConfigurationException(
                    $"Unknown configuration key '{key}'. A key this agent does not know is a "
                        + "control you believe is on and is not, so it refuses to compose. "
                        + "The keys are the builder verbs, camelCased — see docs/how-to.md.");
        }
    }

    // A server is a URL when it needs nothing else, an object when it carries auth — an API key
    // in a named header, or a bearer token (an OAuth access token or PAT). A server with no
    // auth carries none of those keys; a refresh-flow token is code (a delegate) and arrives
    // through UseMcp, never through the document.
    private static void ApplyMcpServer(StandardAgent agent, JsonNode? server, string key)
    {
        if (server is JsonObject)
        {
            agent.Mcp(
                Text(server, key, "endpointUrl"),
                Text(server, key, "relativeUrl", fallback: string.Empty),
                Whole(server, "timeoutSeconds", fallback: 30),
                OptionalText(server, "bearerToken"),
                OptionalText(server, "apiKey"),
                Text(server, key, "apiKeyHeader", fallback: "X-Api-Key"));

            return;
        }

        agent.Mcp(Text(server, key));
    }

    private static string? OptionalText(JsonNode? node, string property) =>
        (node as JsonObject)?[property]?.GetValue<string>();

    private static string Text(JsonNode? node, string key) =>
        node?.GetValueKind() is JsonValueKind.String
            ? node.GetValue<string>()
            : throw new InvalidAgentConfigurationException(
                $"'{key}' must be a string.");

    private static string Text(JsonNode? node, string key, string property, string? fallback = null)
    {
        JsonNode? value = (node as JsonObject)?[property];

        if (value is null)
        {
            return fallback
                ?? throw new InvalidAgentConfigurationException(
                    $"'{key}' needs a '{property}'.");
        }

        return value.GetValueKind() is JsonValueKind.String
            ? value.GetValue<string>()
            : throw new InvalidAgentConfigurationException(
                $"'{key}.{property}' must be a string.");
    }

    private static string[] Texts(JsonNode? node, string key) =>
        [.. Items(node, key).Select(item => Text(item, key))];

    private static IEnumerable<JsonNode?> Items(JsonNode? node, string key) =>
        node as JsonArray
            ?? throw new InvalidAgentConfigurationException(
                $"'{key}' must be an array.");

    private static IEnumerable<KeyValuePair<string, JsonNode?>> Entries(JsonNode? node, string key) =>
        node as JsonObject
            ?? throw new InvalidAgentConfigurationException(
                $"'{key}' must be an object.");

    private static bool Truth(JsonNode? node, string key) =>
        node?.GetValueKind() switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,

            _ => throw new InvalidAgentConfigurationException(
                $"'{key}' must be true or false.")
        };

    private static double Number(JsonNode? node, string property, double fallback) =>
        (node as JsonObject)?[property]?.GetValue<double>() ?? fallback;

    private static int Whole(JsonNode? node, string key) =>
        node?.GetValueKind() is JsonValueKind.Number
            ? node.GetValue<int>()
            : throw new InvalidAgentConfigurationException(
                $"'{key}' must be a number.");

    private static int Whole(JsonNode? node, string property, int fallback) =>
        (node as JsonObject)?[property]?.GetValue<int>() ?? fallback;

    private static int? OptionalWhole(JsonNode? node, string property) =>
        (node as JsonObject)?[property]?.GetValue<int>();

    private static decimal? OptionalMoney(JsonNode? node, string property) =>
        (node as JsonObject)?[property]?.GetValue<decimal>();

    private static TimeSpan? OptionalSeconds(JsonNode? node, string property) =>
        (node as JsonObject)?[property] is JsonNode value
            ? TimeSpan.FromSeconds(value.GetValue<double>())
            : null;

    private static TEnum Named<TEnum>(JsonNode? node, string key) where TEnum : struct =>
        NamedText<TEnum>(Text(node, key), key);

    private static TEnum NamedText<TEnum>(string name, string key) where TEnum : struct =>
        Enum.TryParse(name, ignoreCase: true, out TEnum parsed)
            ? parsed
            : throw new InvalidAgentConfigurationException(
                $"'{key}' does not accept '{name}'. It accepts: "
                    + string.Join(", ", Enum.GetNames(typeof(TEnum))) + ".");
}
