// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net.Http.Headers;
using System.Text.Json;
using RESTFulSense.Clients;
using Standard.Agents.Models.Brokers.Verifiers;

namespace Standard.Agents.Brokers.Verifiers;

public sealed class VerifierBroker : IVerifierBroker
{
    private const string ChatCompletionsRelativeUrl = "chat/completions";
    private const string JsonMediaType = "application/json";

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly IRESTFulApiFactoryClient apiClient;
    private readonly string model;
    private readonly double temperature;
    private readonly int maxTokens;
    private readonly string systemPrompt;

    public VerifierBroker(
        string apiUrl,
        string apiKey,
        string model,
        double temperature,
        int maxTokens,
        int timeoutSeconds,
        string systemPrompt)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiUrl),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(scheme: "Bearer", parameter: apiKey);

        this.apiClient = new RESTFulApiFactoryClient(httpClient);
        this.model = model;
        this.temperature = temperature;
        this.maxTokens = maxTokens;
        this.systemPrompt = systemPrompt;
    }

    public async ValueTask<string> VerifyAsync(string candidate)
    {
        ChatCompletionRequest chatCompletionRequest = new(
            Model: this.model,
            Messages:
            [
                new ChatMessage(Role: "system", Content: this.systemPrompt),
                new ChatMessage(Role: "user", Content: candidate)
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
