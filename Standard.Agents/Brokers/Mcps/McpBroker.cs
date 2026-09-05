// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using RESTFulSense.Clients;
using Standard.Agents.Models.Brokers.Mcps;

namespace Standard.Agents.Brokers.Mcps;

public sealed class McpBroker : IMcpBroker
{
    private const string JsonMediaType = "application/json";
    private const string JsonRpcVersion = "2.0";
    private const string ToolsCallMethod = "tools/call";
    private const string ToolsListMethod = "tools/list";
    private const string OpenObjectSchema = "{}";

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
        int timeoutSeconds,
        string? bearerToken = null,
        string? apiKey = null,
        string apiKeyHeader = "X-Api-Key",
        Func<ValueTask<string>>? bearerTokenProvider = null)
    {
        // A dynamic token rides a handler so every request asks the provider — that is what an
        // OAuth access token needs, because the one from composition time expires. Static
        // credentials ride the default headers; the provider, when present, wins over both.
        var httpClient = bearerTokenProvider is null
            ? new HttpClient()
            : new HttpClient(new BearerTokenHandler(bearerTokenProvider));

        httpClient.BaseAddress = new Uri(endpointUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        if (bearerTokenProvider is null && string.IsNullOrEmpty(bearerToken) is false)
        {
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
        }

        if (string.IsNullOrEmpty(apiKey) is false)
        {
            httpClient.DefaultRequestHeaders.Add(apiKeyHeader, apiKey);
        }

        this.apiClient = new RESTFulApiFactoryClient(httpClient);
        this.relativeUrl = relativeUrl;
    }

    private sealed class BearerTokenHandler : DelegatingHandler
    {
        private readonly Func<ValueTask<string>> provideToken;

        public BearerTokenHandler(Func<ValueTask<string>> provideToken)
            : base(new HttpClientHandler()) =>
            this.provideToken = provideToken;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", await this.provideToken());

            return await base.SendAsync(request, cancellationToken);
        }
    }

    public async ValueTask<string> CallAsync(string name, string argumentsJson)
    {
        JsonRpcRequest jsonRpcRequest = new(
            JsonRpc: JsonRpcVersion,
            Id: Interlocked.Increment(ref this.requestId),
            Method: ToolsCallMethod,
            Params: new ToolCallParams(
                Name: name,
                Arguments: JsonNode.Parse(argumentsJson)));

        JsonRpcResponse jsonRpcResponse =
            await PostAsync<JsonRpcRequest, JsonRpcResponse>(
                this.relativeUrl,
                jsonRpcRequest);

        return ToText(jsonRpcResponse);
    }

    public async ValueTask<IReadOnlyList<McpTool>> ListToolsAsync()
    {
        JsonRpcRequest jsonRpcRequest = new(
            JsonRpc: JsonRpcVersion,
            Id: Interlocked.Increment(ref this.requestId),
            Method: ToolsListMethod,
            Params: null);

        JsonRpcToolListResponse jsonRpcResponse =
            await PostAsync<JsonRpcRequest, JsonRpcToolListResponse>(
                this.relativeUrl,
                jsonRpcRequest);

        if (jsonRpcResponse.Error is not null)
        {
            throw new HttpRequestException(jsonRpcResponse.Error.Message);
        }

        return [.. (jsonRpcResponse.Result?.Tools ?? []).Select(tool =>
            new McpTool(
                tool.Name,
                tool.Description ?? string.Empty,
                tool.InputSchema?.GetRawText() ?? OpenObjectSchema))];
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
        TContent content)
    {
        return await this.apiClient.PostContentAsync<TContent, TResult>(
            relativeUrl,
            content,
            mediaType: JsonMediaType,
            serializationFunction: async value =>
                JsonSerializer.Serialize(value, jsonOptions),
            deserializationFunction: async json =>
                JsonSerializer.Deserialize<TResult>(json, jsonOptions)!);
    }
}
