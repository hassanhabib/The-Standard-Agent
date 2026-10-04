// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json.Nodes;
using Standard.Agents.Models.Brokers.Mcps;

namespace Standard.Agents.Brokers.Mcps;

public sealed class StdioMcpBroker : IMcpBroker
{
    private const string JsonRpcVersion = "2.0";
    private const string ToolsListMethod = "tools/list";
    private const string InitializeMethod = "initialize";
    private const string InitializedMethod = "notifications/initialized";
    private const string LatestProtocolVersion = "2025-06-18";
    private const string ClientName = "Standard.Agents";
    private const string OpenObjectSchema = "{}";

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

    public ValueTask<string> CallAsync(string name, string argumentsJson) =>
        throw new NotImplementedException();

    public async ValueTask<IReadOnlyList<McpTool>> ListToolsAsync()
    {
        JsonNode? result = await RequestAsync(ToolsListMethod, parameters: null);
        JsonArray tools = result?["tools"] as JsonArray ?? [];

        return [.. tools.Select(tool =>
            new McpTool(
                tool!["name"]!.GetValue<string>(),
                tool["description"]?.GetValue<string>() ?? string.Empty,
                tool["inputSchema"]?.ToJsonString() ?? OpenObjectSchema))];
    }

    private async ValueTask<JsonNode?> RequestAsync(string method, JsonNode? parameters)
    {
        await this.exchangeLock.WaitAsync();

        try
        {
            await EnsureInitializedAsync();

            return await ExchangeAsync(method, parameters);
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

        await ExchangeAsync(InitializeMethod, initializeParameters);
        await WriteAsync(new JsonObject { ["jsonrpc"] = JsonRpcVersion, ["method"] = InitializedMethod });
        this.isInitialized = true;
    }

    private async ValueTask<JsonNode?> ExchangeAsync(string method, JsonNode? parameters)
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

            JsonObject? message = JsonNode.Parse(line) as JsonObject;

            if (message?["id"]?.GetValue<int>() == id)
            {
                return message["result"];
            }
        }
    }

    private async ValueTask WriteAsync(JsonObject message)
    {
        await this.serverInput.WriteLineAsync(message.ToJsonString());
        await this.serverInput.FlushAsync();
    }
}
