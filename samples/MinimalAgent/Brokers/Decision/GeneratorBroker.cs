// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MinimalAgent.Brokers.Decision;

public sealed class GeneratorBroker : IGeneratorBroker
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient httpClient;
    private readonly string model;

    public GeneratorBroker(string apiUrl, string apiKey, string model)
    {
        this.httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiUrl),
            Timeout = TimeSpan.FromSeconds(120)
        };

        this.httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        this.model = model;
    }

    public async ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt)
    {
        ChatRequest request = new(
            Model: this.model,
            Messages:
            [
                new ChatMessage("system", systemPrompt),
                new ChatMessage("user", userPrompt)
            ],
            Stream: false);

        HttpResponseMessage httpResponse =
            await this.httpClient.PostAsJsonAsync("chat/completions", request, jsonOptions);

        httpResponse.EnsureSuccessStatusCode();

        ChatResponse? response =
            await httpResponse.Content.ReadFromJsonAsync<ChatResponse>(jsonOptions);

        return response!.Choices[0].Message.Content;
    }

    private sealed record ChatRequest(
        string Model,
        IReadOnlyList<ChatMessage> Messages,
        bool Stream);

    private sealed record ChatMessage(string Role, string Content);

    private sealed record ChatResponse(IReadOnlyList<ChatChoice> Choices);

    private sealed record ChatChoice(ChatMessage Message);
}
