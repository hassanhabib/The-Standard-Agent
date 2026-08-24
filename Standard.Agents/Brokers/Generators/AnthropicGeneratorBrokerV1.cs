// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Standard.Agents.Models.Brokers.Generators.V1;

namespace Standard.Agents.Brokers.Generators;

// Native tool calling against the Anthropic Messages API — the same IGeneratorBrokerV1 seam as
// the OpenAI-compatible broker, under a different wire shape: system prompts travel as a
// top-level `system` field, tools carry `input_schema`, the model's choice comes back as
// `tool_use` content blocks, and a tool's result returns as a `tool_result` block inside a user
// message. Usage comes back reported (`input_tokens`/`output_tokens`), never guessed.
//
// Like its sibling, the request is built as a JSON tree: the tool schemas are already JSON the
// host wrote, and re-modelling them would only give us a second place for them to be wrong.
public sealed class AnthropicGeneratorBrokerV1 : IGeneratorBrokerV1
{
    private const string MessagesRelativeUrl = "v1/messages";
    private const string JsonMediaType = "application/json";

    // The Messages API refuses a request without a version pin, which is the right default for
    // a wire format that evolves: the shape this broker speaks is the shape it names.
    private const string AnthropicVersion = "2023-06-01";

    private readonly HttpClient httpClient;
    private readonly string model;
    private readonly double temperature;
    private readonly int maxTokens;

    public AnthropicGeneratorBrokerV1(
        string apiKey,
        string model,
        double temperature = 0.7,
        int maxTokens = 1024,
        int timeoutSeconds = 120,
        string apiUrl = "https://api.anthropic.com/")
    {
        this.httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiUrl),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        this.httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        this.httpClient.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);

        this.model = model;
        this.temperature = temperature;
        this.maxTokens = maxTokens;
    }

    public async ValueTask<GenerationResult> GenerateAsync(
        IReadOnlyList<ConversationMessage> messages,
        IReadOnlyList<ToolDefinition> tools)
    {
        JsonObject request = BuildRequest(messages, tools);

        using var content = new StringContent(
            request.ToJsonString(), Encoding.UTF8, JsonMediaType);

        using HttpResponseMessage response =
            await this.httpClient.PostAsync(MessagesRelativeUrl, content);

        response.EnsureSuccessStatusCode();

        JsonNode? body = JsonNode.Parse(await response.Content.ReadAsStringAsync());

        return ReadResult(body);
    }

    private JsonObject BuildRequest(
        IReadOnlyList<ConversationMessage> messages,
        IReadOnlyList<ToolDefinition> tools)
    {
        var request = new JsonObject
        {
            ["model"] = this.model,
            ["temperature"] = this.temperature,
            ["max_tokens"] = this.maxTokens,
            ["messages"] = RenderMessages(messages)
        };

        // System prompts are a top-level field here, not a message role; several collapse into
        // one the way the model will read them anyway - in order.
        string system = string.Join(
            "\n\n",
            messages
                .Where(message => message.Role is MessageRole.System)
                .Select(message => message.Content)
                .Where(text => string.IsNullOrWhiteSpace(text) is false));

        if (string.IsNullOrEmpty(system) is false)
        {
            request["system"] = system;
        }

        if (tools.Count > 0)
        {
            request["tools"] = new JsonArray([.. tools.Select(ToJson)]);
        }

        return request;
    }

    // The API alternates user and assistant turns, and a tool's result is a `tool_result`
    // block inside a USER message that must directly follow the assistant `tool_use` it
    // answers — so consecutive tool results merge into one user message rather than each
    // claiming a turn of its own.
    private static JsonArray RenderMessages(IReadOnlyList<ConversationMessage> messages)
    {
        var rendered = new JsonArray();
        var pendingToolResults = new JsonArray();

        foreach (ConversationMessage message in messages)
        {
            if (message.Role is MessageRole.Tool)
            {
                pendingToolResults.Add((JsonNode)new JsonObject
                {
                    ["type"] = "tool_result",
                    ["tool_use_id"] = message.ToolCallId,
                    ["content"] = message.Content
                });

                continue;
            }

            FlushToolResults(rendered, ref pendingToolResults);

            if (message.Role is MessageRole.System)
            {
                continue;
            }

            rendered.Add((JsonNode)new JsonObject
            {
                ["role"] = message.Role.ToString().ToLowerInvariant(),
                ["content"] = RenderContent(message)
            });
        }

        FlushToolResults(rendered, ref pendingToolResults);

        return rendered;
    }

    private static void FlushToolResults(JsonArray rendered, ref JsonArray pendingToolResults)
    {
        if (pendingToolResults.Count is 0)
        {
            return;
        }

        rendered.Add((JsonNode)new JsonObject
        {
            ["role"] = "user",
            ["content"] = pendingToolResults
        });

        pendingToolResults = [];
    }

    private static JsonNode RenderContent(ConversationMessage message)
    {
        if (message.ToolCalls.Count is 0)
        {
            return JsonValue.Create(message.Content)!;
        }

        var blocks = new JsonArray();

        if (string.IsNullOrWhiteSpace(message.Content) is false)
        {
            blocks.Add((JsonNode)new JsonObject
            {
                ["type"] = "text",
                ["text"] = message.Content
            });
        }

        foreach (ModelToolCall call in message.ToolCalls)
        {
            blocks.Add((JsonNode)new JsonObject
            {
                ["type"] = "tool_use",
                ["id"] = call.Id,
                ["name"] = call.Name,
                ["input"] = ParseJsonOrEmpty(call.ArgumentsJson)
            });
        }

        return blocks;
    }

    private static JsonNode ToJson(ToolDefinition tool) =>
        new JsonObject
        {
            ["name"] = tool.Name,
            ["description"] = tool.Description,
            ["input_schema"] = ParseSchema(tool.ParametersJson)
        };

    // Same posture as the sibling broker: a schema that does not parse degrades to an
    // argument-less tool rather than failing the whole turn on one bad schema.
    private static JsonNode ParseSchema(string parametersJson)
    {
        JsonNode parsed = ParseJsonOrEmpty(parametersJson);

        if (parsed is JsonObject schema && schema.ContainsKey("type") is false)
        {
            schema["type"] = "object";
        }

        return parsed;
    }

    private static JsonNode ParseJsonOrEmpty(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new JsonObject();
        }

        try
        {
            return JsonNode.Parse(json) ?? new JsonObject();
        }
        catch (JsonException)
        {
            return new JsonObject();
        }
    }

    private static GenerationResult ReadResult(JsonNode? body)
    {
        JsonNode? usage = body?["usage"];

        var text = new StringBuilder();
        List<ModelToolCall> toolCalls = [];

        if (body?["content"] is JsonArray blocks)
        {
            foreach (JsonNode? block in blocks)
            {
                switch (block?["type"]?.GetValue<string>())
                {
                    case "text":
                        text.Append(block?["text"]?.GetValue<string>());

                        break;

                    case "tool_use":
                        string id = block?["id"]?.GetValue<string>() ?? string.Empty;
                        string name = block?["name"]?.GetValue<string>() ?? string.Empty;

                        string arguments =
                            block?["input"]?.ToJsonString() ?? "{}";

                        if (string.IsNullOrWhiteSpace(name) is false)
                        {
                            toolCalls.Add(new ModelToolCall(id, name, arguments));
                        }

                        break;
                }
            }
        }

        return new GenerationResult
        {
            Content = text.ToString(),
            ToolCalls = toolCalls,
            PromptTokens = usage?["input_tokens"]?.GetValue<int>() ?? 0,
            CompletionTokens = usage?["output_tokens"]?.GetValue<int>() ?? 0
        };
    }
}
