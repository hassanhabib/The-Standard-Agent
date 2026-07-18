// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using RESTFulSense.Clients;
using Standard.Agents.Models.Brokers.Generators;

namespace Standard.Agents.Brokers.Generators;

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
    {
        this.httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiUrl),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        this.httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(scheme: "Bearer", parameter: apiKey);

        this.apiClient = new RESTFulApiFactoryClient(this.httpClient);
        this.model = model;
        this.temperature = temperature;
        this.maxTokens = maxTokens;
    }

    public async ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt)
    {
        ChatCompletionRequest chatCompletionRequest = new(
            Model: this.model,
            Messages:
            [
                new ChatMessage(Role: "system", Content: systemPrompt),
                new ChatMessage(Role: "user", Content: userPrompt)
            ],
            Stream: false,
            Temperature: this.temperature,
            MaxTokens: this.maxTokens);

        ChatCompletionResponse chatCompletionResponse =
            await PostAsync<ChatCompletionRequest, ChatCompletionResponse>(
                ChatCompletionsRelativeUrl,
                chatCompletionRequest);

        return chatCompletionResponse.Choices[0].Message.Content;
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ChatCompletionRequest chatCompletionRequest = new(
            Model: this.model,
            Messages:
            [
                new ChatMessage(Role: "system", Content: systemPrompt),
                new ChatMessage(Role: "user", Content: userPrompt)
            ],
            Stream: true,
            Temperature: this.temperature,
            MaxTokens: this.maxTokens);

        string requestJson = JsonSerializer.Serialize(chatCompletionRequest, jsonOptions);

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
    }
