// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using RESTFulSense.Clients;
using Standard.Agents.Models.Brokers.Generators;

namespace Standard.Agents.Brokers.Generators;

// The request is built as a JSON tree rather than through typed records — converged onto the
// construction V1 already uses, for the reason V1 already documents: growing optional record
// fields answers one request and re-opens the same question at the next one
// (docs/per-request-inference.md §5.1).
public sealed class GeneratorBroker : IGeneratorBroker
{
    private const string ChatCompletionsRelativeUrl = "chat/completions";

    private const string JsonMediaType = "application/json";
    private const string DataFieldPrefix = "data:";
    private const string DoneSentinel = "[DONE]";

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly IRESTFulApiFactoryClient apiClient;
    private readonly HttpClient httpClient;
    private readonly string model;
    private readonly double temperature;
    private readonly int maxTokens;

    public GeneratorBroker(
        string apiUrl,
        string apiKey,
        string model,
        double temperature,
        int maxTokens,
        int timeoutSeconds)
        : this(new HttpClient(), apiUrl, apiKey, model, temperature, maxTokens, timeoutSeconds)
    {
    }

    // The host's handler under this broker's traffic (F-23). The handler is the host's: the
    // client around it holds nothing of its own and never disposes it.
    public GeneratorBroker(
        HttpMessageHandler handler,
        string apiUrl,
        string apiKey,
        string model,
        double temperature,
        int maxTokens,
        int timeoutSeconds)
        : this(
            new HttpClient(handler, disposeHandler: false),
            apiUrl,
            apiKey,
            model,
            temperature,
            maxTokens,
            timeoutSeconds)
    {
    }

    private GeneratorBroker(
        HttpClient httpClient,
        string apiUrl,
        string apiKey,
        string model,
        double temperature,
        int maxTokens,
        int timeoutSeconds)
    {
        this.httpClient = httpClient;
        this.httpClient.BaseAddress = new Uri(apiUrl);
        this.httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        this.httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(scheme: "Bearer", parameter: apiKey);

        this.apiClient = new RESTFulApiFactoryClient(this.httpClient);
        this.model = model;
        this.temperature = temperature;
        this.maxTokens = maxTokens;
    }

    /// <summary>This broker puts resolved inference options on the wire.</summary>
    public bool HonorsRequest => true;

    public ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt) =>
        GenerateAsync(systemPrompt, userPrompt, inference: null);

    ValueTask<string> IGeneratorBroker.GenerateAsync(
        string systemPrompt,
        string userPrompt,
        ResolvedInference inference) =>
        GenerateAsync(systemPrompt, userPrompt, inference);

    private async ValueTask<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        ResolvedInference? inference)
    {
        JsonObject request = BuildRequest(systemPrompt, userPrompt, stream: false, inference);

        ChatCompletionResponse chatCompletionResponse =
            await PostAsync<JsonObject, ChatCompletionResponse>(
                ChatCompletionsRelativeUrl,
                request);

        return chatCompletionResponse.Choices[0].Message.Content;
    }

    public IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default) =>
        GenerateStreamAsync(systemPrompt, userPrompt, inference: null, cancellationToken);

    IAsyncEnumerable<string> IGeneratorBroker.GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        ResolvedInference inference,
        CancellationToken cancellationToken) =>
        GenerateStreamAsync(systemPrompt, userPrompt, inference, cancellationToken);

    private async IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        ResolvedInference? inference,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        JsonObject request = BuildRequest(systemPrompt, userPrompt, stream: true, inference);

        string requestJson = request.ToJsonString();

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            ChatCompletionsRelativeUrl)
        {
            Content = new StringContent(requestJson, Encoding.UTF8, JsonMediaType)
        };

        using HttpResponseMessage httpResponse = await this.httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        httpResponse.EnsureSuccessStatusCode();

        await using Stream responseStream =
            await httpResponse.Content.ReadAsStreamAsync(cancellationToken);

        using var reader = new StreamReader(responseStream);

        while (true)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);

            if (line is null)
            {
                break;
            }

            if (line.StartsWith(DataFieldPrefix) is false)
            {
                continue;
            }

            string data = line[DataFieldPrefix.Length..].Trim();

            if (data == DoneSentinel)
            {
                break;
            }

            ChatCompletionChunk? chunk =
                JsonSerializer.Deserialize<ChatCompletionChunk>(data, jsonOptions);

            string? content = chunk?.Choices is { Count: > 0 }
                ? chunk.Choices[0].Delta.Content
                : null;

            if (string.IsNullOrEmpty(content) is false)
            {
                yield return content;
            }
        }
    }

    private JsonObject BuildRequest(
        string systemPrompt,
        string userPrompt,
        bool stream,
        ResolvedInference? inference)
    {
        var request = new JsonObject
        {
            ["model"] = this.model,

            ["messages"] = new JsonArray(
                new JsonObject { ["role"] = "system", ["content"] = systemPrompt },
                new JsonObject { ["role"] = "user", ["content"] = userPrompt }),

            ["stream"] = stream,
            ["temperature"] = inference?.Temperature ?? this.temperature,
            ["max_tokens"] = inference?.MaxTokens ?? this.maxTokens
        };

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

    private async ValueTask<TResult> PostAsync<TContent, TResult>(
        string relativeUrl,
        TContent content)
    {
        return await this.apiClient.PostContentAsync<TContent, TResult>(
            relativeUrl,
            content,
            mediaType: JsonMediaType,
            serializationFunction: async value =>
                value is JsonObject node
                    ? node.ToJsonString()
                    : JsonSerializer.Serialize(value, jsonOptions),
            deserializationFunction: async json =>
                JsonSerializer.Deserialize<TResult>(json, jsonOptions)!);
    }
}
