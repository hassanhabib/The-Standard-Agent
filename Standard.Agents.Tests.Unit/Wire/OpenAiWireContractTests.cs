// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Models.Brokers.Generators.V1;
using Xunit;

namespace Standard.Agents.Tests.Unit.Wire;

// Found in the 2026-09-04 principal review (F-21): the native OpenAI-compatible broker was only
// ever exercised through scripted doubles, so the route, the headers, the request shape, the
// response variants and the failure modes of the real wire could drift unseen. These run the
// real broker against a scripted protocol server and read the bytes.
public class OpenAiWireContractTests
{
    private const string BaseUrl = "http://provider.test/v1/";

    private static readonly IReadOnlyList<ConversationMessage> conversation =
    [
        new() { Role = MessageRole.System, Content = "be brief" },
        new() { Role = MessageRole.User, Content = "what is owed on 42?" },

        new()
        {
            Role = MessageRole.Assistant,
            ToolCalls = [new ModelToolCall("call_1", "lookup", "{\"account\":\"42\"}")]
        },

        new() { Role = MessageRole.Tool, Content = "owed: 12", ToolCallId = "call_1" }
    ];

    private static readonly IReadOnlyList<ToolDefinition> tools =
    [
        new ToolDefinition(
            Name: "lookup",
            Description: "looks up an account",
            ParametersJson: "{\"type\":\"object\",\"properties\":{\"account\":{\"type\":\"string\"}}}")
    ];

    private static GeneratorBrokerV1 CreateBroker(ScriptedServerHandler server, int timeoutSeconds = 30) =>
        new(server, BaseUrl, "key-1", "model-1", temperature: 0.2, maxTokens: 64, timeoutSeconds);

    [Fact]
    public async Task ShouldPostAChatCompletionInTheOpenAiShapeAsync()
    {
        // given — a server answering with a tool call and usage
        ScriptedServerHandler server = ScriptedServerHandler.Answering(
            "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":null,"
                + "\"tool_calls\":[{\"id\":\"call_2\",\"type\":\"function\","
                + "\"function\":{\"name\":\"lookup\",\"arguments\":\"{\\\"account\\\":\\\"7\\\"}\"}}]}}],"
                + "\"usage\":{\"prompt_tokens\":21,\"completion_tokens\":8}}");

        GeneratorBrokerV1 broker = CreateBroker(server);

        var expectedResult = new GenerationResult
        {
            Content = "",
            ToolCalls = [new ModelToolCall("call_2", "lookup", "{\"account\":\"7\"}")],
            PromptTokens = 21,
            CompletionTokens = 8
        };

        // when
        GenerationResult actualResult = await broker.GenerateAsync(conversation, tools);

        // then — the route, the credential, the media type
        HttpRequestMessage request = server.Requests.Should().ContainSingle().Subject;
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri.Should().Be(new Uri("http://provider.test/v1/chat/completions"));
        request.Headers.Authorization?.Scheme.Should().Be("Bearer");
        request.Headers.Authorization?.Parameter.Should().Be("key-1");
        request.Content?.Headers.ContentType?.MediaType.Should().Be("application/json");

        // and the request body, as the provider reads it
        JsonNode body = JsonNode.Parse(server.Bodies[0])!;
        body["model"]!.GetValue<string>().Should().Be("model-1");
        body["temperature"]!.GetValue<double>().Should().Be(0.2);
        body["max_tokens"]!.GetValue<int>().Should().Be(64);

        JsonArray messages = body["messages"]!.AsArray();
        messages.Count.Should().Be(4);
        messages[0]!["role"]!.GetValue<string>().Should().Be("system");
        messages[1]!["role"]!.GetValue<string>().Should().Be("user");
        messages[2]!["role"]!.GetValue<string>().Should().Be("assistant");
        messages[2]!["tool_calls"]![0]!["id"]!.GetValue<string>().Should().Be("call_1");
        messages[2]!["tool_calls"]![0]!["type"]!.GetValue<string>().Should().Be("function");

        messages[2]!["tool_calls"]![0]!["function"]!["arguments"]!.GetValue<string>()
            .Should().Be("{\"account\":\"42\"}");

        messages[3]!["role"]!.GetValue<string>().Should().Be("tool");
        messages[3]!["tool_call_id"]!.GetValue<string>().Should().Be("call_1");

        JsonNode tool = body["tools"]![0]!;
        tool["type"]!.GetValue<string>().Should().Be("function");
        tool["function"]!["name"]!.GetValue<string>().Should().Be("lookup");
        tool["function"]!["parameters"]!["type"]!.GetValue<string>().Should().Be("object");

        // and the response, read back whole
        actualResult.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task ShouldNotAdvertiseAnEmptyToolListAsync()
    {
        // given — no tools; some providers treat "tools": [] as a different request
        ScriptedServerHandler server = ScriptedServerHandler.Answering(
            "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"12\"}}]}");

        GeneratorBrokerV1 broker = CreateBroker(server);

        // when
        GenerationResult actualResult = await broker.GenerateAsync(conversation, tools: []);

        // then
        JsonNode body = JsonNode.Parse(server.Bodies[0])!;
        body["tools"].Should().BeNull();
        actualResult.Content.Should().Be("12");
    }

    [Fact]
    public async Task ShouldReadAnAnswerWithoutUsageAsZeroTokensAsync()
    {
        // given — a partial answer: no usage block, no tool calls, content only
        ScriptedServerHandler server = ScriptedServerHandler.Answering(
            "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"12\"}}]}");

        GeneratorBrokerV1 broker = CreateBroker(server);

        var expectedResult = new GenerationResult { Content = "12" };

        // when
        GenerationResult actualResult = await broker.GenerateAsync(conversation, tools);

        // then — reported, not estimated: zero says "the provider did not say"
        actualResult.Should().BeEquivalentTo(expectedResult);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task ShouldThrowHttpRequestExceptionOnAFailedStatusAsync(HttpStatusCode statusCode)
    {
        // given — the provider refusing, throttling or failing
        ScriptedServerHandler server = ScriptedServerHandler.AnsweringWith(
            statusCode,
            "{\"error\":{\"message\":\"no\"}}");

        GeneratorBrokerV1 broker = CreateBroker(server);

        // when
        Func<Task> generateAsync = async () => await broker.GenerateAsync(conversation, tools);

        // then — a native exception, for the foundation above to localize
        (await generateAsync.Should().ThrowAsync<HttpRequestException>())
            .Which.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public async Task ShouldThrowJsonExceptionOnAMalformedBodyAsync()
    {
        // given — a 200 that is not JSON (a proxy's HTML page, a truncated stream)
        ScriptedServerHandler server = ScriptedServerHandler.Answering(
            "<html>bad gateway</html>",
            mediaType: "text/html");

        GeneratorBrokerV1 broker = CreateBroker(server);

        // when
        Func<Task> generateAsync = async () => await broker.GenerateAsync(conversation, tools);

        // then
        await generateAsync.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task ShouldTimeOutWhenTheProviderNeverAnswersAsync()
    {
        // given — a server that holds the connection past the broker's timeout
        ScriptedServerHandler server = ScriptedServerHandler.Hanging();
        GeneratorBrokerV1 broker = CreateBroker(server, timeoutSeconds: 1);

        // when
        Func<Task> generateAsync = async () => await broker.GenerateAsync(conversation, tools);

        // then — the timeout is the broker's, so a hung provider cannot hang the run
        await generateAsync.Should().ThrowAsync<TaskCanceledException>();
    }
}
