// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using RESTFulSense.Clients;

namespace Standard.Agents.Brokers.Direction;

public sealed class McpBroker : IMcpBroker
{
    private const string JsonMediaType = "application/json";
    private const string JsonRpcVersion = "2.0";
    private const string ToolsCallMethod = "tools/call";
    private const string InputArgumentName = "input";

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IRESTFulApiFactoryClient apiClient;
    private readonly string relativeUrl;
    private int requestId;

    public McpBroker(
        string endpointUrl,
        string relativeUrl,
        int timeoutSeconds)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(endpointUrl),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        this.apiClient = new RESTFulApiFactoryClient(httpClient);
        this.relativeUrl = relativeUrl;
    }

    public async ValueTask<string> CallAsync(string name, string input)
    {
        JsonRpcRequest jsonRpcRequest = new(
            JsonRpc: JsonRpcVersion,
            Id: Interlocked.Increment(ref this.requestId),
            Method: ToolsCallMethod,
            Params: new ToolCallParams(
                Name: name,
                Arguments: new Dictionary<string, string>
                {
                    [InputArgumentName] = input
                }));

        JsonRpcResponse jsonRpcResponse =
            await PostAsync<JsonRpcRequest, JsonRpcResponse>(
                this.relativeUrl,
                jsonRpcRequest);

        return ToText(jsonRpcResponse);
    }

                        private static string ToText(JsonRpcResponse jsonRpcResponse)
    {
        if (jsonRpcResponse.Error is not null)
        {
            throw new HttpRequestException(jsonRpcResponse.Error.Message);
        }

        return string.Concat(
            jsonRpcResponse.Result!.Content.Select(content => content.Text));
    }

    private async ValueTask<TResult> PostAsync<TContent, TResult>(
        string relativeUrl,
        TContent content) =>
        await this.apiClient.PostContentAsync<TContent, TResult>(
            relativeUrl,
            content,
            mediaType: JsonMediaType,
            serializationFunction: value =>
                ValueTask.FromResult(JsonSerializer.Serialize(value, jsonOptions)),
            deserializationFunction: json =>
                ValueTask.FromResult(JsonSerializer.Deserialize<TResult>(json, jsonOptions)!));

    private sealed record JsonRpcRequest(
        [property: JsonPropertyName("jsonrpc")] string JsonRpc,
        int Id,
        string Method,
        ToolCallParams Params);

    private sealed record ToolCallParams(
        string Name,
        IReadOnlyDictionary<string, string> Arguments);

    private sealed record JsonRpcResponse(
        ToolCallResult? Result,
        JsonRpcError? Error);

    private sealed record ToolCallResult(
        IReadOnlyList<ToolCallContent> Content);

    private sealed record ToolCallContent(
        string Type,
        string Text);

    private sealed record JsonRpcError(
        int Code,
        string Message);
}
