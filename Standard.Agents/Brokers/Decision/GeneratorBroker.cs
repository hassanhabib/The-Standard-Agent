using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Standard.Agents.Brokers.Decision;

// The Brain's liaison: a thin OpenAI-compatible client (POST /v1/chat/completions).
// Constructs the native request from primitives, POSTs, returns the completion text.
// No flow control, no authored prompts — those are Data.
public sealed class GeneratorBroker : IGeneratorBroker
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient httpClient;
    private readonly string model;
    private readonly double temperature;
    private readonly int maxTokens;

    public GeneratorBroker(
        string apiUrl,
        string apiKey,
        string model,
        double temperature,
        int maxTokens)
    {
        this.httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiUrl),
            Timeout = TimeSpan.FromSeconds(120)
        };

        this.httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        this.model = model;
        this.temperature = temperature;
        this.maxTokens = maxTokens;
    }

    public async ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt)
    {
        ChatCompletionRequest request = new(
            Model: this.model,
            Messages:
            [
                new ChatMessage("system", systemPrompt),
                new ChatMessage("user", userPrompt)
            ],
            Stream: false,
            Temperature: this.temperature,
            MaxTokens: this.maxTokens);

        HttpResponseMessage httpResponse =
            await this.httpClient.PostAsJsonAsync("chat/completions", request, jsonOptions);

        httpResponse.EnsureSuccessStatusCode();

        ChatCompletionResponse? response =
            await httpResponse.Content.ReadFromJsonAsync<ChatCompletionResponse>(jsonOptions);

        return response!.Choices[0].Message.Content;
    }

    private sealed record ChatCompletionRequest(
        string Model,
        IReadOnlyList<ChatMessage> Messages,
        bool Stream,
        double Temperature,
        int MaxTokens);

    private sealed record ChatMessage(string Role, string Content);

    private sealed record ChatCompletionResponse(IReadOnlyList<ChatChoice> Choices);

    private sealed record ChatChoice(ChatMessage Message);
}
