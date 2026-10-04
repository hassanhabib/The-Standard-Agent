// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using RESTFulSense.Services;
using Standard.Agents.Models.Brokers.Mcps;

namespace Standard.Agents.Brokers.Mcps;

public sealed class McpBroker : IMcpBroker
{
    private const string JsonMediaType = "application/json";
    private const string EventStreamMediaType = "text/event-stream";
    private const string DataFieldPrefix = "data:";
    private const string JsonRpcVersion = "2.0";
    private const string ToolsCallMethod = "tools/call";
    private const string ToolsListMethod = "tools/list";
    private const string InitializeMethod = "initialize";
    private const string InitializedMethod = "notifications/initialized";
    private const string LatestProtocolVersion = "2025-06-18";
    private const string ClientName = "Standard.Agents";
    private const string SessionIdHeader = "Mcp-Session-Id";
    private const string ProtocolVersionHeader = "MCP-Protocol-Version";
    private const string OpenObjectSchema = "{}";

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient httpClient;
    private readonly string relativeUrl;
    private readonly SemaphoreSlim initializationLock = new(initialCount: 1, maxCount: 1);
    private int requestId;
    private bool isInitialized;
    private string? sessionId;
    private string? protocolVersion;

    public McpBroker(
        string endpointUrl,
        string relativeUrl,
        int timeoutSeconds,
        string? bearerToken = null,
        string? apiKey = null,
        string apiKeyHeader = "X-Api-Key",
        Func<ValueTask<string>>? bearerTokenProvider = null)
        : this(
            new HttpClientHandler(),
            endpointUrl,
            relativeUrl,
            timeoutSeconds,
            bearerToken,
            apiKey,
            apiKeyHeader,
            bearerTokenProvider)
    {
    }

    // The host's handler under this broker's traffic (F-23). The handler is the host's: the
    // client around it holds nothing of its own and never disposes it. A dynamic token rides a
    // handler of its own on top, so every request asks the provider — that is what an OAuth
    // access token needs, because the one from composition time expires. Static credentials
    // ride the default headers; the provider, when present, wins over both.
    public McpBroker(
        HttpMessageHandler handler,
        string endpointUrl,
        string relativeUrl,
        int timeoutSeconds,
        string? bearerToken,
        string? apiKey,
        string apiKeyHeader,
        Func<ValueTask<string>>? bearerTokenProvider)
    {
        var httpClient = bearerTokenProvider is null
            ? new HttpClient(handler, disposeHandler: false)
            : new HttpClient(
                new BearerTokenHandler(bearerTokenProvider, handler),
                disposeHandler: false);

        httpClient.BaseAddress = new Uri(endpointUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        httpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(JsonMediaType));

        httpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(EventStreamMediaType));

        if (bearerTokenProvider is null && string.IsNullOrEmpty(bearerToken) is false)
        {
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
        }

        if (string.IsNullOrEmpty(apiKey) is false)
        {
            httpClient.DefaultRequestHeaders.Add(apiKeyHeader, apiKey);
        }

        this.httpClient = httpClient;
        this.relativeUrl = relativeUrl;
    }

    private sealed class BearerTokenHandler : DelegatingHandler
    {
        private readonly Func<ValueTask<string>> provideToken;

        public BearerTokenHandler(
            Func<ValueTask<string>> provideToken,
            HttpMessageHandler innerHandler)
            : base(innerHandler) =>
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
        await EnsureInitializedAsync();

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
        await EnsureInitializedAsync();

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

    private async ValueTask EnsureInitializedAsync()
    {
        if (this.isInitialized)
        {
            return;
        }

        await this.initializationLock.WaitAsync();

        try
        {
            if (this.isInitialized is false)
            {
                await InitializeAsync();
                this.isInitialized = true;
            }
        }
        finally
        {
            this.initializationLock.Release();
        }
    }

    private async ValueTask InitializeAsync()
    {
        JsonRpcRequest initializeRequest = new(
            JsonRpc: JsonRpcVersion,
            Id: Interlocked.Increment(ref this.requestId),
            Method: InitializeMethod,
            Params: new InitializeParams(
                ProtocolVersion: LatestProtocolVersion,
                Capabilities: new JsonObject(),
                ClientInfo: new ClientInfo(
                    Name: ClientName,
                    Version: typeof(McpBroker).Assembly.GetName().Version?.ToString() ?? string.Empty)));

        using var timeout = new CancellationTokenSource(this.httpClient.Timeout);

        using HttpResponseMessage initializeHttpResponse =
            await SendAsync(this.relativeUrl, initializeRequest, timeout.Token);

        JsonRpcInitializeResponse initializeResponse =
            await ReadAsync<JsonRpcInitializeResponse>(initializeHttpResponse, timeout.Token);

        if (initializeResponse.Error is not null)
        {
            throw new HttpRequestException(initializeResponse.Error.Message);
        }

        this.sessionId = initializeHttpResponse.Headers.TryGetValues(
            SessionIdHeader,
            out IEnumerable<string>? sessionIds)
                ? sessionIds.First()
                : null;

        this.protocolVersion = initializeResponse.Result?.ProtocolVersion;

        JsonRpcRequest initializedNotification = new(
            JsonRpc: JsonRpcVersion,
            Id: null,
            Method: InitializedMethod,
            Params: null);

        await NotifyAsync(this.relativeUrl, initializedNotification);
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
        using var timeout = new CancellationTokenSource(this.httpClient.Timeout);
        string? sentSessionId = this.sessionId;

        using HttpResponseMessage httpResponse =
            await SendAsync(relativeUrl, content, timeout.Token);

        if (IsExpiredSession(httpResponse, sentSessionId))
        {
            await RestartSessionAsync(sentSessionId!);

            using HttpResponseMessage retriedHttpResponse =
                await SendAsync(relativeUrl, content, timeout.Token);

            return await ReadAsync<TResult>(retriedHttpResponse, timeout.Token);
        }

        return await ReadAsync<TResult>(httpResponse, timeout.Token);
    }

    private static bool IsExpiredSession(HttpResponseMessage httpResponse, string? sentSessionId) =>
        httpResponse.StatusCode is HttpStatusCode.NotFound && sentSessionId is not null;

    private async ValueTask RestartSessionAsync(string expiredSessionId)
    {
        await this.initializationLock.WaitAsync();

        try
        {
            if (this.sessionId == expiredSessionId)
            {
                this.sessionId = null;
                this.protocolVersion = null;
                await InitializeAsync();
            }
        }
        finally
        {
            this.initializationLock.Release();
        }
    }

    private static async ValueTask<TResult> ReadAsync<TResult>(
        HttpResponseMessage httpResponse,
        CancellationToken cancellationToken)
    {
        await ValidationService.ValidateHttpResponseAsync(httpResponse);

        string responseJson = IsEventStream(httpResponse)
            ? await ReadResponseFromEventStreamAsync(httpResponse, cancellationToken)
            : await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        return JsonSerializer.Deserialize<TResult>(responseJson, jsonOptions)!;
    }

    private async ValueTask NotifyAsync<TContent>(string relativeUrl, TContent content)
    {
        using var timeout = new CancellationTokenSource(this.httpClient.Timeout);

        using HttpResponseMessage httpResponse =
            await SendAsync(relativeUrl, content, timeout.Token);
    }

    private async ValueTask<HttpResponseMessage> SendAsync<TContent>(
        string relativeUrl,
        TContent content,
        CancellationToken cancellationToken)
    {
        string requestJson = JsonSerializer.Serialize(content, jsonOptions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, relativeUrl)
        {
            Content = new StringContent(requestJson, Encoding.UTF8, JsonMediaType)
        };

        if (this.sessionId is not null)
        {
            httpRequest.Headers.Add(SessionIdHeader, this.sessionId);
        }

        if (this.protocolVersion is not null)
        {
            httpRequest.Headers.Add(ProtocolVersionHeader, this.protocolVersion);
        }

        return await this.httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
    }

    private static bool IsEventStream(HttpResponseMessage httpResponse) =>
        httpResponse.Content.Headers.ContentType?.MediaType == EventStreamMediaType;

    private static async ValueTask<string> ReadResponseFromEventStreamAsync(
        HttpResponseMessage httpResponse,
        CancellationToken cancellationToken)
    {
        await using Stream responseStream =
            await httpResponse.Content.ReadAsStreamAsync(cancellationToken);

        using var reader = new StreamReader(responseStream);
        var eventData = new StringBuilder();

        while (await reader.ReadLineAsync(cancellationToken) is string line)
        {
            if (line.StartsWith(DataFieldPrefix))
            {
                eventData.AppendLine(ToFieldValue(line));

                continue;
            }

            if (line.Length is 0 && IsResponse(eventData.ToString()))
            {
                return eventData.ToString();
            }

            if (line.Length is 0)
            {
                eventData.Clear();
            }
        }

        return IsResponse(eventData.ToString())
            ? eventData.ToString()
            : throw new HttpRequestException(
                "The MCP server closed its event stream without a response.");
    }

    private static string ToFieldValue(string line)
    {
        string value = line[DataFieldPrefix.Length..];

        return value.StartsWith(' ')
            ? value[1..]
            : value;
    }

    private static bool IsResponse(string eventData)
    {
        if (string.IsNullOrWhiteSpace(eventData))
        {
            return false;
        }

        JsonObject? message = JsonNode.Parse(eventData) as JsonObject;

        return message?.ContainsKey("result") is true || message?.ContainsKey("error") is true;
    }
}
