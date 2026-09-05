// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;
using Standard.Agents.Models.Brokers.Agents;
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
            ApplyEntry(agent, key, value);
        }

        return agent;
    }

    // The document reaches the same verbs code does, so a verb's refusal is rewrapped into the
    // document's own exception naming the entry the author must fix — they are looking at a JSON
    // file, not a stack trace — with the verb's refusal preserved beneath it.
    private static void ApplyEntry(StandardAgent agent, string key, JsonNode? value)
    {
        try
        {
            Apply(agent, key, value);
        }
        catch (InvalidAgentApiUrlException invalidAgentApiUrlException)
        {
            throw new InvalidAgentConfigurationException(
                message:
                    $"The '{key}' entry's 'apiUrl' is not an endpoint base the route can be "
                        + "appended to. " + invalidAgentApiUrlException.Message,
                innerException: invalidAgentApiUrlException);
        }
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
            // Identity rides in the document, so the file IS the agent: a registry reads the
            // name a handoff calls and the description that advertises it from the same JSON
            // that composes it. Each key preserves the other, so their order cannot matter.
            case "name":
                agent.Identity(Text(value, key), agent.Description);

                break;

            case "description":
                agent.Identity(agent.Name, Text(value, key));

                break;

            case "brain":
                Shaped(value, key, "apiUrl", "apiKey", "model", "temperature", "maxTokens", "timeoutSeconds");

                agent.Brain(
                    Text(value, key, "apiUrl"),
                    Text(value, key, "apiKey", fallback: string.Empty),
                    Text(value, key, "model"),
                    Number(value, key, "temperature", fallback: 0.7),
                    PositiveWhole(value, key, "maxTokens", fallback: 1024),
                    PositiveWhole(value, key, "timeoutSeconds", fallback: 120));

                break;

            case "nativeBrain":
                Shaped(value, key, "apiUrl", "apiKey", "model", "temperature", "maxTokens");

                agent.NativeBrain(
                    Text(value, key, "apiUrl"),
                    Text(value, key, "apiKey", fallback: string.Empty),
                    Text(value, key, "model"),
                    Number(value, key, "temperature", fallback: 0.7),
                    PositiveWhole(value, key, "maxTokens", fallback: 1024));

                break;

            case "nativeBrainAnthropic":
                Shaped(value, key, "apiKey", "model", "temperature", "maxTokens");

                agent.NativeBrainAnthropic(
                    Text(value, key, "apiKey"),
                    Text(value, key, "model"),
                    Number(value, key, "temperature", fallback: 0.7),
                    PositiveWhole(value, key, "maxTokens", fallback: 1024));

                break;

            case "skills" when value is JsonArray:
                foreach (JsonNode? source in Listed(value, key))
                {
                    agent.Skills(Text(source, key));
                }

                break;

            case "skills":
                agent.Skills(Text(value, key));

                break;

            case "knowledge" when value is JsonObject:
                Shaped(value, key, "path", "pattern", "maxResults", "minScore");

                agent.Knowledge(
                    Text(value, key, "path"),
                    Text(value, key, "pattern", fallback: "*.md"),
                    PositiveWhole(value, key, "maxResults", fallback: 3),
                    Number(value, key, "minScore", fallback: 0.0));

                break;

            case "knowledge":
                agent.Knowledge(Text(value, key));

                break;

            // A document that wants no memory says so with one key: a hosted agent is the
            // whole document, and a hosted memory is one memory for every caller.
            case "memory" when value is JsonValue memoryFlag
                && memoryFlag.TryGetValue(out bool memoryEnabled):
                if (memoryEnabled is false)
                {
                    agent.WithoutMemory();
                }

                break;

            case "memory":
                agent.Memory(Text(value, key));

                break;

            case "mcp" when value is JsonArray:
                foreach (JsonNode? server in Listed(value, key))
                {
                    ApplyMcpServer(agent, server, key);
                }

                break;

            case "mcp":
                ApplyMcpServer(agent, value, key);

                break;

            case "agents" when value is JsonArray:
                foreach (JsonNode? member in Listed(value, key))
                {
                    ApplyFleetMember(agent, member, key);
                }

                break;

            case "agents":
                agent.Agents(Text(value, key));

                break;

            case "gate":
                Shaped(value, key, "apiUrl", "apiKey", "model", "temperature", "maxTokens", "timeoutSeconds");

                agent.Gate(
                    Text(value, key, "apiUrl"),
                    Text(value, key, "apiKey", fallback: string.Empty),
                    Text(value, key, "model"),
                    Number(value, key, "temperature", fallback: 0.0),
                    PositiveWhole(value, key, "maxTokens", fallback: 16),
                    PositiveWhole(value, key, "timeoutSeconds", fallback: 30));

                break;

            case "ruleGate":
                agent.RuleGate(ListedTexts(value, key));

                break;

            case "judge":
                Shaped(value, key, "apiUrl", "apiKey", "model", "temperature", "maxTokens", "timeoutSeconds");

                agent.Judge(
                    Text(value, key, "apiUrl"),
                    Text(value, key, "apiKey", fallback: string.Empty),
                    Text(value, key, "model"),
                    Number(value, key, "temperature", fallback: 0.0),
                    PositiveWhole(value, key, "maxTokens", fallback: 16),
                    PositiveWhole(value, key, "timeoutSeconds", fallback: 30));

                break;

            case "ruleJudge":
                agent.RuleJudge(ListedTexts(value, key));

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
                Shaped(rules, key, "rules");

                agent.Redact([.. Listed(rules["rules"], "redact.rules").Select(rule =>
                    new RedactionRule
                    {
                        Label = Text(Shaped(rule, "redact.rules", "label", "pattern"), "redact.rules", "label"),
                        Pattern = Text(rule, "redact.rules", "pattern")
                    })]);

                break;

            case "redact" when Truth(value, key):
                agent.Redact();

                break;

            case "redact":
                break;

            case "maxTurns":
                agent.MaxTurns(Positive(Whole(value, key), key));

                break;

            case "allowTools":
                agent.AllowTools(Texts(value, key));

                break;

            case "permissions":
                agent.Permissions(Named<PermissionMode>(value, key));

                break;

            case "risk":
                foreach ((string levelName, JsonNode? toolNames) in ListedEntries(value, key))
                {
                    agent.Risk(NamedText<RiskLevel>(levelName, key), Texts(toolNames, key));
                }

                break;

            case "requireApproval":
                agent.RequireApproval(ListedTexts(value, key));

                break;

            case "logTo" when value is JsonObject:
                Shaped(value, key, "path", "verbosity");

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

            case "auditPayloads" when Truth(value, key):
                agent.AuditPayloads();

                break;

            case "auditPayloads":
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
                Shaped(value, key, "path", "maxHistoryTurns");

                agent.Sessions(
                    Text(value, key, "path"),
                    PositiveWhole(value, key, "maxHistoryTurns", fallback: 20));

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
                ApplyBudget(agent, budgetNode: value);

                break;

            case "usage":
                Shaped(value, key, "charactersPerToken");

                agent.Usage(
                    PositiveNumber(
                        Number(value, key, "charactersPerToken", fallback: 4.0),
                        $"{key}.charactersPerToken"));

                break;

            case "resilience" when value is JsonObject:
                Shaped(value, key, "retries");

                agent.Resilience(
                    NonNegative(Whole(value, key, "retries", fallback: 3), $"{key}.retries"));

                break;

            case "resilience":
                agent.Resilience(NonNegative(Whole(value, key), key));

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

    // The builder is the one place the budget's rules live; the document reaches it through the
    // same verb code does. A refusal there is rewrapped into the document's own exception —
    // naming the entries the author must fix, with the builder's refusal preserved beneath it —
    // so a cost bound with no rate fails the way a typo'd key does: loudly, and by name.
    private static void ApplyBudget(StandardAgent agent, JsonNode? budgetNode)
    {
        const string key = "budget";

        Shaped(budgetNode, key, "maxTokens", "maxCostUsd", "maxWallClockSeconds", "costPerThousandTokens");

        try
        {
            agent.Budget(
                maxTokens: Positive(OptionalWhole(budgetNode, key, "maxTokens"), $"{key}.maxTokens"),
                maxCostUsd: Positive(OptionalMoney(budgetNode, key, "maxCostUsd"), $"{key}.maxCostUsd"),
                maxWallClock: Positive(
                    OptionalSeconds(budgetNode, key, "maxWallClockSeconds"),
                    $"{key}.maxWallClockSeconds"),
                costPerThousandTokens: NonNegative(
                    OptionalMoney(budgetNode, key, "costPerThousandTokens") ?? 0m,
                    $"{key}.costPerThousandTokens"));
        }
        catch (InvalidAgentBudgetException invalidAgentBudgetException)
        {
            throw new InvalidAgentConfigurationException(
                message:
                    "The 'budget' entry sets 'maxCostUsd' without a positive "
                        + "'costPerThousandTokens'. Cost is priced off the token count times that "
                        + "rate, so without it the spend is zero forever and the bound can never "
                        + "trip. Add your model's rate, or bound by 'maxTokens' instead.",
                innerException: invalidAgentBudgetException);
        }
    }

    // A fleet member is a path when the agents live beside the process (a folder of agent
    // documents), an object when the agent rides inline — the same document FromJson composes,
    // identity included, because the document IS the agent. An inline member must be named: a
    // handoff calls agents by name, and a nameless agent is one the brain could never call.
    private static void ApplyFleetMember(StandardAgent agent, JsonNode? member, string key)
    {
        if (member is JsonObject)
        {
            StandardAgent composed = FromJson(member.ToJsonString());

            if (string.IsNullOrWhiteSpace(composed.Name))
            {
                throw new InvalidAgentConfigurationException(
                    $"An inline '{key}' entry needs a 'name' — a handoff calls agents by name.");
            }

            IReadOnlyList<RegisteredAgent> one =
                [new RegisteredAgent(composed.Name, composed.Description, composed)];

            agent.OnAgents(() => new ValueTask<IReadOnlyList<RegisteredAgent>>(one));

            return;
        }

        agent.Agents(Text(member, key));
    }

    // A server is a URL when it needs nothing else, an object when it carries auth — an API key
    // in a named header, or a bearer token (an OAuth access token or PAT). A server with no
    // auth carries none of those keys; a refresh-flow token is code (a delegate) and arrives
    // through UseMcp, never through the document.
    private static void ApplyMcpServer(StandardAgent agent, JsonNode? server, string key)
    {
        if (server is JsonObject)
        {
            Shaped(
                server,
                key,
                "endpointUrl",
                "relativeUrl",
                "timeoutSeconds",
                "bearerToken",
                "apiKey",
                "apiKeyHeader");

            agent.Mcp(
                Text(server, key, "endpointUrl"),
                Text(server, key, "relativeUrl", fallback: string.Empty),
                PositiveWhole(server, key, "timeoutSeconds", fallback: 30),
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

    // A section's shape, checked before its values are read (principal review 2026-09-04,
    // F-24): it must be an object, and every key in it must be one the section accepts. A
    // nested typo used to be ignored, which is a control the author believes is on and is not,
    // the same failure the top-level unknown-key refusal exists to prevent.
    private static JsonObject Shaped(JsonNode? node, string key, params string[] properties)
    {
        JsonObject section = node as JsonObject
            ?? throw new InvalidAgentConfigurationException($"'{key}' must be an object.");

        foreach (string property in section.Select(entry => entry.Key))
        {
            if (properties.Contains(property) is false)
            {
                throw new InvalidAgentConfigurationException(
                    $"'{key}' does not accept '{property}'. It accepts: "
                        + string.Join(", ", properties) + ".");
            }
        }

        return section;
    }

    // A control that lists nothing is not a control: a gate with no rules, an approval list
    // with no tools, a fleet with no agents all read as on and do nothing.
    private static IEnumerable<JsonNode?> Listed(JsonNode? node, string key)
    {
        JsonArray items = node as JsonArray
            ?? throw new InvalidAgentConfigurationException($"'{key}' must be an array.");

        if (items.Count is 0)
        {
            throw new InvalidAgentConfigurationException(ListsNothing(key));
        }

        return items;
    }

    private static string[] ListedTexts(JsonNode? node, string key) =>
        [.. Listed(node, key).Select(item => Text(item, key))];

    private static IEnumerable<KeyValuePair<string, JsonNode?>> ListedEntries(JsonNode? node, string key)
    {
        JsonObject entries = node as JsonObject
            ?? throw new InvalidAgentConfigurationException($"'{key}' must be an object.");

        if (entries.Count is 0)
        {
            throw new InvalidAgentConfigurationException(ListsNothing(key));
        }

        return entries;
    }

    private static string ListsNothing(string key) =>
        $"'{key}' lists nothing. A control that lists nothing is not a control: remove the "
            + "key, or list what it should hold.";

    // A limit of zero or less composes as if it were set and never binds - or, in MaxTurns'
    // case, is quietly clamped. The document refuses it by name instead.
    private static int Positive(int value, string name) =>
        value > 0
            ? value
            : throw new InvalidAgentConfigurationException($"'{name}' must be a positive number.");

    private static int? Positive(int? value, string name) =>
        value is null ? null : Positive(value.Value, name);

    private static decimal? Positive(decimal? value, string name) =>
        value is null or > 0
            ? value
            : throw new InvalidAgentConfigurationException($"'{name}' must be a positive number.");

    private static TimeSpan? Positive(TimeSpan? value, string name) =>
        value is null || value > TimeSpan.Zero
            ? value
            : throw new InvalidAgentConfigurationException($"'{name}' must be a positive number.");

    private static double PositiveNumber(double value, string name) =>
        value > 0
            ? value
            : throw new InvalidAgentConfigurationException($"'{name}' must be a positive number.");

    private static int NonNegative(int value, string name) =>
        value >= 0
            ? value
            : throw new InvalidAgentConfigurationException(
                $"'{name}' must be zero or a positive number.");

    private static decimal NonNegative(decimal value, string name) =>
        value >= 0
            ? value
            : throw new InvalidAgentConfigurationException(
                $"'{name}' must be zero or a positive number.");

    private static int PositiveWhole(JsonNode? node, string key, string property, int fallback) =>
        Positive(Whole(node, key, property, fallback), $"{key}.{property}");

    // Numbers are read as numbers or refused by name; a string where a number belongs used to
    // surface as a runtime exception with no entry named, or fall through to a default.
    private static JsonNode? NumberNode(JsonNode? node, string key, string property)
    {
        JsonNode? value = (node as JsonObject)?[property];

        if (value is null)
        {
            return null;
        }

        return value.GetValueKind() is JsonValueKind.Number
            ? value
            : throw new InvalidAgentConfigurationException(
                $"'{key}.{property}' must be a number.");
    }

    private static double Number(JsonNode? node, string key, string property, double fallback) =>
        NumberNode(node, key, property)?.GetValue<double>() ?? fallback;

    private static int Whole(JsonNode? node, string key) =>
        node?.GetValueKind() is JsonValueKind.Number
            ? node.GetValue<int>()
            : throw new InvalidAgentConfigurationException(
                $"'{key}' must be a number.");

    private static int Whole(JsonNode? node, string key, string property, int fallback) =>
        NumberNode(node, key, property)?.GetValue<int>() ?? fallback;

    private static int? OptionalWhole(JsonNode? node, string key, string property) =>
        NumberNode(node, key, property)?.GetValue<int>();

    private static decimal? OptionalMoney(JsonNode? node, string key, string property) =>
        NumberNode(node, key, property)?.GetValue<decimal>();

    private static TimeSpan? OptionalSeconds(JsonNode? node, string key, string property) =>
        NumberNode(node, key, property) is JsonNode value
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
