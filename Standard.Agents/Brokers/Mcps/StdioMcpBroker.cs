// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;
using Standard.Agents.Models.Brokers.Mcps;

namespace Standard.Agents.Brokers.Mcps;

public sealed class StdioMcpBroker : IMcpBroker
{
    private const string JsonRpcVersion = "2.0";
    private const string ToolsListMethod = "tools/list";
    private const string ToolsCallMethod = "tools/call";
    private const string InitializeMethod = "initialize";
    private const string InitializedMethod = "notifications/initialized";
    private const string LatestProtocolVersion = "2025-06-18";
    private const string ClientName = "Standard.Agents";
    private const string OpenObjectSchema = "{}";
    private const string PingMethod = "ping";
    private const int MethodNotFoundCode = -32601;

    private readonly TextReader serverOutput;
    private readonly TextWriter serverInput;
    private readonly TimeSpan timeout;
    private readonly SemaphoreSlim exchangeLock = new(initialCount: 1, maxCount: 1);
    private int requestId;
    private bool isInitialized;

    public StdioMcpBroker(TextReader serverOutput, TextWriter serverInput, int timeoutSeconds)
    {
        this.serverOutput = serverOutput;
        this.serverInput = serverInput;
        this.timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    public async ValueTask<string> CallAsync(string name, string argumentsJson)
    {
        var parameters = new JsonObject
        {
            ["name"] = name,
            ["arguments"] = JsonNode.Parse(argumentsJson)
        };

        JsonNode? result = await RequestAsync(ToolsCallMethod, parameters);
        JsonArray content = result?["content"] as JsonArray ?? [];
        string text = string.Concat(content.Select(ToText));

        return text.Length is 0 && result?["structuredContent"] is JsonNode structuredContent
            ? structuredContent.ToJsonString()
            : text;
    }

    private static string ToText(JsonNode? content)
    {
        string? type = content?["type"]?.GetValue<string>();

        return type switch
        {
            "text" => content!["text"]?.GetValue<string>() ?? string.Empty,
            "resource" => content!["resource"]?["text"]?.GetValue<string>()
                ?? $"[resource {content["resource"]?["uri"]?.GetValue<string>()}]",
            "resource_link" =>
                $"[resource_link {content!["name"]?.GetValue<string>()}: {content["uri"]?.GetValue<string>()}]",
            _ => $"[{type} {content?["mimeType"]?.GetValue<string>()}]"
        };
    }

    public async ValueTask<IReadOnlyList<McpTool>> ListToolsAsync()
    {
        List<McpTool> tools = [];
        HashSet<string> visitedCursors = [];
        string? cursor = null;

        do
        {
            JsonObject? parameters = cursor is null
                ? null
                : new JsonObject { ["cursor"] = cursor };

            JsonNode? page = await RequestAsync(ToolsListMethod, parameters);
            JsonArray pageTools = page?["tools"] as JsonArray ?? [];

            tools.AddRange(pageTools.Select(tool =>
                new McpTool(
                    tool!["name"]!.GetValue<string>(),
                    tool["description"]?.GetValue<string>() ?? string.Empty,
                    tool["inputSchema"]?.ToJsonString() ?? OpenObjectSchema)));

            cursor = page?["nextCursor"]?.GetValue<string>();
        }
        while (string.IsNullOrEmpty(cursor) is false && visitedCursors.Add(cursor));

        return tools;
    }

    private async ValueTask<JsonNode?> RequestAsync(string method, JsonNode? parameters)
    {
        await this.exchangeLock.WaitAsync();

        try
        {
            await EnsureInitializedAsync();
            JsonObject answer = await ExchangeAsync(method, parameters);

            return answer["error"] is JsonNode error
                ? throw new HttpRequestException(error["message"]?.GetValue<string>())
                : answer["result"];
        }
        finally
        {
            this.exchangeLock.Release();
        }
    }

    private async ValueTask EnsureInitializedAsync()
    {
        if (this.isInitialized)
        {
            return;
        }

        var initializeParameters = new JsonObject
        {
            ["protocolVersion"] = LatestProtocolVersion,
            ["capabilities"] = new JsonObject(),
            ["clientInfo"] = new JsonObject
            {
                ["name"] = ClientName,
                ["version"] = typeof(StdioMcpBroker).Assembly.GetName().Version?.ToString() ?? string.Empty
            }
        };

        JsonObject answer = await ExchangeAsync(InitializeMethod, initializeParameters);
        int? errorCode = answer["error"]?["code"]?.GetValue<int>();

        if (errorCode is not null and not MethodNotFoundCode)
        {
            throw new HttpRequestException(answer["error"]!["message"]?.GetValue<string>());
        }

        if (errorCode is null)
        {
            await WriteAsync(new JsonObject { ["jsonrpc"] = JsonRpcVersion, ["method"] = InitializedMethod });
        }

        this.isInitialized = true;
    }

    private async ValueTask<JsonObject> ExchangeAsync(string method, JsonNode? parameters)
    {
        int id = ++this.requestId;

        var request = new JsonObject
        {
            ["jsonrpc"] = JsonRpcVersion,
            ["id"] = id,
            ["method"] = method
        };

        if (parameters is not null)
        {
            request["params"] = parameters;
        }

        await WriteAsync(request);

        using var timeoutSource = new CancellationTokenSource(this.timeout);

        while (true)
        {
            string line = await this.serverOutput.ReadLineAsync(timeoutSource.Token)
                ?? throw new HttpRequestException("The MCP server ended its output before answering.");

            JsonObject? message = ToMessage(line);

            if (message?.ContainsKey("method") is true)
            {
                await AnswerServerAsync(message);

                continue;
            }

            if (IsAnswerTo(message, id))
            {
                return message!;
            }
        }
    }

    private static JsonObject? ToMessage(string line)
    {
        try
        {
            return JsonNode.Parse(line) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsAnswerTo(JsonObject? message, int id) =>
        message?["id"] is JsonValue answerId
            && answerId.TryGetValue(out int answeredId)
            && answeredId == id;

    private async ValueTask AnswerServerAsync(JsonObject serverMessage)
    {
        if (serverMessage["id"] is null)
        {
            return;
        }

        var answer = new JsonObject
        {
            ["jsonrpc"] = JsonRpcVersion,
            ["id"] = serverMessage["id"]!.DeepClone()
        };

        if (serverMessage["method"]?.GetValue<string>() is PingMethod)
        {
            answer["result"] = new JsonObject();
        }
        else
        {
            answer["error"] = new JsonObject
            {
                ["code"] = MethodNotFoundCode,
                ["message"] = "The client does not offer this method."
            };
        }

        await WriteAsync(answer);
    }

    private async ValueTask WriteAsync(JsonObject message)
    {
        await this.serverInput.WriteLineAsync(message.ToJsonString());
        await this.serverInput.FlushAsync();
    }
}
