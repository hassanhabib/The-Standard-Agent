// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using RESTFulSense.Clients;

namespace Standard.Agents.Brokers.Decision;

// The Judge's substrate. Separately configured from the Brain so a deployment can
// honour invariant 7.6 — no self-certification — without touching the library.
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

    public VerifierBroker(
        string apiUrl,
        string apiKey,
        string model,
        double temperature,
        int maxTokens,
        int timeoutSeconds)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiUrl),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        this.apiClient = new RESTFulApiFactoryClient(httpClient);
        this.model = model;
        this.temperature = temperature;
        this.maxTokens = maxTokens;
    }

    public async ValueTask<double> VerifyAsync(string systemPrompt, string candidate)
    {
        ChatCompletionRequest chatCompletionRequest = new(
            Model: this.model,
            Messages:
            [
                new ChatMessage("system", systemPrompt),
                new ChatMessage("user", candidate)
            ],
            Stream: false,
            Temperature: this.temperature,
            MaxTokens: this.maxTokens);

        ChatCompletionResponse chatCompletionResponse =
            await PostAsync<ChatCompletionRequest, ChatCompletionResponse>(
                ChatCompletionsRelativeUrl,
                chatCompletionRequest);

        return ToScore(chatCompletionResponse.Choices[0].Message.Content);
    }

    // Constructing the declared native from the wire's primitive — a broker's job.
    // Unparseable text throws FormatException; JudgeService localizes it. Coercing a
    // bad score to 0.0 or 1.0 here would be the broker inventing a verdict, and a
    // fabricated 1.0 would silently approve output no judge ever passed.
    // Range is NOT enforced here: that is a rule, and rules live in JudgeService.
    private static double ToScore(string content) =>
        double.Parse(
            content.Trim(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture);

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

    private sealed record ChatCompletionRequest(
        string Model,
        IReadOnlyList<ChatMessage> Messages,
        bool Stream,
        double Temperature,
        int MaxTokens);

    private sealed record ChatMessage(
        string Role,
        string Content);

    private sealed record ChatCompletionResponse(
        IReadOnlyList<ChatChoice> Choices);

    private sealed record ChatChoice(
        ChatMessage Message);
}
