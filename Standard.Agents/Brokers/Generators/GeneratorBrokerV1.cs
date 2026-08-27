// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Standard.Agents.Models.Brokers.Generators;
using Standard.Agents.Models.Brokers.Generators.V1;

namespace Standard.Agents.Brokers.Generators;

// Native tool calling against an OpenAI-compatible endpoint: tools go out as `tools[]`, the
// model's choice comes back as `tool_calls`, and usage comes back reported rather than guessed.
//
// The request is built as a JSON tree rather than through typed records, because the tool
// schemas are already JSON the host wrote and re-modelling them would only give us a second
// place for them to be wrong.
public sealed class GeneratorBrokerV1 : IGeneratorBrokerV1
{
    private const string ChatCompletionsRelativeUrl = "chat/completions";
    private const string JsonMediaType = "application/json";

    private readonly HttpClient httpClient;
    private readonly string model;
    private readonly double temperature;
    private readonly int maxTokens;

    public GeneratorBrokerV1(
        string apiUrl,
        string apiKey,
        string model,
        double temperature = 0.7,
        int maxTokens = 1024,
        int timeoutSeconds = 120)
    {
        this.httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiUrl),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        this.httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(scheme: "Bearer", parameter: apiKey);

        this.model = model;
        this.temperature = temperature;
        this.maxTokens = maxTokens;
    }

    public ValueTask<GenerationResult> GenerateAsync(
        IReadOnlyList<ConversationMessage> messages,
        IReadOnlyList<ToolDefinition> tools) =>
        GenerateAsync(messages, tools, inference: null);

    /// <summary>This broker puts resolved inference options on the wire.</summary>
    public bool HonorsRequest => true;

    ValueTask<GenerationResult> IGeneratorBrokerV1.GenerateAsync(
        IReadOnlyList<ConversationMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        ResolvedInference inference) =>
        GenerateAsync(messages, tools, inference);

    private async ValueTask<GenerationResult> GenerateAsync(
        IReadOnlyList<ConversationMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        ResolvedInference? inference)
    {
        JsonObject request = BuildRequest(messages, tools, inference);

        using var content = new StringContent(
            request.ToJsonString(), Encoding.UTF8, JsonMediaType);

        using HttpResponseMessage response =
            await this.httpClient.PostAsync(ChatCompletionsRelativeUrl, content);

        response.EnsureSuccessStatusCode();

        JsonNode? body = JsonNode.Parse(await response.Content.ReadAsStringAsync());

        return ReadResult(body);
    }

    private JsonObject BuildRequest(
        IReadOnlyList<ConversationMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        ResolvedInference? inference)
    {
        var request = new JsonObject
        {
            ["model"] = this.model,
            ["temperature"] = inference?.Temperature ?? this.temperature,
            ["max_tokens"] = inference?.MaxTokens ?? this.maxTokens,
            ["messages"] = new JsonArray([.. messages.Select(ToJson)])
        };

        // Only advertise when there is something to advertise: an empty tools array is a
        // different request than no tools at all on some providers.
        if (tools.Count > 0)
        {
            request["tools"] = new JsonArray([.. tools.Select(ToJson)]);
        }

        WriteInference(request, inference);

        return request;
    }

    // The resolved options, written as the wire knows them. The values arrive already
    // precedence-resolved at the boundary; this broker writes what it is given and decides
    // nothing (docs/per-request-inference.md §4.2).
    private static void WriteInference(JsonObject request, ResolvedInference? inference)
    {
        if (inference is null)
        {
            return;
        }

        if (inference.Seed is int seed)
        {
            request["seed"] = seed;
        }

        if (inference.Stop.Count > 0)
        {
            request["stop"] = new JsonArray([.. inference.Stop.Select(s => (JsonNode)s)]);
        }

        // The schema that survived precedence seeds the wire — and the guardian already holds
        // the same schema, so an engine that quietly ignores response_format degrades to a
        // guarantee rather than to nothing (§4.1).
        if (string.IsNullOrWhiteSpace(inference.ResponseSchemaJson) is false)
        {
            JsonNode? schema = TryParse(inference.ResponseSchemaJson);

            if (schema is not null)
            {
                request["response_format"] = new JsonObject
                {
                    ["type"] = "json_schema",
                    ["json_schema"] = new JsonObject
                    {
                        ["name"] = "response",
                        ["schema"] = schema
                    }
                };
            }
        }

        // Already sanitized at the boundary; merged under the same rule again here, because
        // this is the perimeter and one enforcement point is one forgotten call away from none.
        ProviderOptions.MergeInto(request, inference.ProviderOptionsJson);
    }

    private static JsonNode? TryParse(string json)
    {
        try
        {
            return JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static JsonNode ToJson(ConversationMessage message)
    {
        var json = new JsonObject
        {
            ["role"] = message.Role.ToString().ToLowerInvariant(),
            ["content"] = message.Content
        };

        if (message.Role is MessageRole.Tool)
        {
            json["tool_call_id"] = message.ToolCallId;
        }

        if (message.ToolCalls.Count > 0)
        {
            json["tool_calls"] = new JsonArray([.. message.ToolCalls.Select(call =>
                (JsonNode)new JsonObject
                {
                    ["id"] = call.Id,
                    ["type"] = "function",
                    ["function"] = new JsonObject
                    {
                        ["name"] = call.Name,
                        ["arguments"] = call.ArgumentsJson
                    }
                })]);
        }

        return json;
    }

    private static JsonNode ToJson(ToolDefinition tool) =>
        new JsonObject
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["parameters"] = ParseParameters(tool.ParametersJson)
            }
        };

    // A tool's parameters are the host's JSON schema — and the wire validates that strictly:
    // a hosted provider rejects the WHOLE request when any tool's parameters is not a schema
    // of type object. One bad tool must never take down the turn, so anything that is not an
    // object schema degrades: a schema that forgot its type but has properties is salvaged,
    // everything else becomes the empty object schema (callable with no arguments).
    private static JsonNode ParseParameters(string parametersJson)
    {
        if (string.IsNullOrWhiteSpace(parametersJson))
        {
            return new JsonObject { ["type"] = "object" };
        }

        try
        {
            if (JsonNode.Parse(parametersJson) is JsonObject schema)
            {
                string? schemaType = schema["type"] is JsonValue typeValue
                    ? typeValue.GetValue<string>()
                    : null;

                if (string.Equals(schemaType, "object", StringComparison.Ordinal))
                {
                    return schema;
                }

                if (schemaType is null && schema.ContainsKey("properties"))
                {
                    schema["type"] = "object";

                    return schema;
                }
            }
        }
        catch (JsonException)
        {
        }
        catch (InvalidOperationException)
        {
        }

        return new JsonObject { ["type"] = "object" };
    }

    private static GenerationResult ReadResult(JsonNode? body)
    {
        JsonNode? message = body?["choices"]?[0]?["message"];
        JsonNode? usage = body?["usage"];

        List<ModelToolCall> toolCalls = [];

        if (message?["tool_calls"] is JsonArray calls)
        {
            foreach (JsonNode? call in calls)
            {
                string id = call?["id"]?.GetValue<string>() ?? string.Empty;
                JsonNode? function = call?["function"];

                string name = function?["name"]?.GetValue<string>() ?? string.Empty;

                string arguments =
                    function?["arguments"]?.GetValue<string>() ?? "{}";

                if (string.IsNullOrWhiteSpace(name) is false)
                {
                    toolCalls.Add(new ModelToolCall(id, name, arguments));
                }
            }
        }

        return new GenerationResult
        {
            Content = message?["content"]?.GetValue<string>() ?? string.Empty,
            ToolCalls = toolCalls,
            PromptTokens = usage?["prompt_tokens"]?.GetValue<int>() ?? 0,
            CompletionTokens = usage?["completion_tokens"]?.GetValue<int>() ?? 0
        };
    }
}
