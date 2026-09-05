// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net;
using System.Text.Json.Nodes;
using FluentAssertions;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Models.Brokers.Generators.V1;
using Xunit;

namespace Standard.Agents.Tests.Unit.Wire;

// The Anthropic Messages wire, read from the bytes the real broker sends (F-21): the route, the
// version pin, the top-level system, tool_use and tool_result blocks, and the usage names.
public class AnthropicWireContractTests
{
    private static readonly IReadOnlyList<ConversationMessage> conversation =
    [
        new() { Role = MessageRole.System, Content = "be brief" },
        new() { Role = MessageRole.User, Content = "what is owed on 42?" },

        new()
        {
            Role = MessageRole.Assistant,
            ToolCalls = [new ModelToolCall("toolu_1", "lookup", "{\"account\":\"42\"}")]
        },

        new() { Role = MessageRole.Tool, Content = "owed: 12", ToolCallId = "toolu_1" }
    ];

    private static readonly IReadOnlyList<ToolDefinition> tools =
    [
        new ToolDefinition(
            Name: "lookup",
            Description: "looks up an account",
            ParametersJson: "{\"type\":\"object\",\"properties\":{\"account\":{\"type\":\"string\"}}}")
    ];

    private static AnthropicGeneratorBrokerV1 CreateBroker(ScriptedServerHandler server) =>
        new(
            server,
            apiKey: "key-1",
            model: "claude-x",
            temperature: 0.2,
            maxTokens: 64,
            timeoutSeconds: 30,
            apiUrl: "http://anthropic.test/");

    [Fact]
    public async Task ShouldPostMessagesInTheAnthropicShapeAsync()
    {
        // given — a server answering with a text block, a tool_use block and usage
        ScriptedServerHandler server = ScriptedServerHandler.Answering(
            "{\"content\":[{\"type\":\"text\",\"text\":\"checking \"},"
                + "{\"type\":\"tool_use\",\"id\":\"toolu_2\",\"name\":\"lookup\",\"input\":{\"account\":\"7\"}}],"
                + "\"usage\":{\"input_tokens\":21,\"output_tokens\":8}}");

        AnthropicGeneratorBrokerV1 broker = CreateBroker(server);

        var expectedResult = new GenerationResult
        {
            Content = "checking ",
            ToolCalls = [new ModelToolCall("toolu_2", "lookup", "{\"account\":\"7\"}")],
            PromptTokens = 21,
            CompletionTokens = 8
        };

        // when
        GenerationResult actualResult = await broker.GenerateAsync(conversation, tools);

        // then — the route and the two headers the API refuses a request without
        HttpRequestMessage request = server.Requests.Should().ContainSingle().Subject;
        request.RequestUri.Should().Be(new Uri("http://anthropic.test/v1/messages"));
        request.Headers.GetValues("x-api-key").Should().ContainSingle("key-1");
        request.Headers.GetValues("anthropic-version").Should().ContainSingle("2023-06-01");

        // and the body: system at the top level, never as a message role
        JsonNode body = JsonNode.Parse(server.Bodies[0])!;
        body["model"]!.GetValue<string>().Should().Be("claude-x");
        body["max_tokens"]!.GetValue<int>().Should().Be(64);
        body["system"]!.GetValue<string>().Should().Be("be brief");

        JsonArray messages = body["messages"]!.AsArray();
        messages.Select(message => message!["role"]!.GetValue<string>()).Should().NotContain("system");
        messages.Select(message => message!["role"]!.GetValue<string>()).Should().NotContain("tool");

        // the assistant's call as a tool_use block, the answer as a tool_result block in a USER turn
        string rendered = server.Bodies[0];
        rendered.Should().Contain("\"type\":\"tool_use\"").And.Contain("\"id\":\"toolu_1\"");
        rendered.Should().Contain("\"type\":\"tool_result\"").And.Contain("\"tool_use_id\":\"toolu_1\"");

        JsonNode tool = body["tools"]![0]!;
        tool["name"]!.GetValue<string>().Should().Be("lookup");
        tool["input_schema"]!["type"]!.GetValue<string>().Should().Be("object");

        // and the response, read back whole
        actualResult.Should().BeEquivalentTo(expectedResult);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ShouldThrowHttpRequestExceptionOnAFailedStatusAsync(HttpStatusCode statusCode)
    {
        // given
        ScriptedServerHandler server = ScriptedServerHandler.AnsweringWith(
            statusCode,
            "{\"type\":\"error\",\"error\":{\"type\":\"rate_limit_error\"}}");

        AnthropicGeneratorBrokerV1 broker = CreateBroker(server);

        // when
        Func<Task> generateAsync = async () => await broker.GenerateAsync(conversation, tools);

        // then
        (await generateAsync.Should().ThrowAsync<HttpRequestException>())
            .Which.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public async Task ShouldReadAnAnswerWithNoBlocksAsEmptyAsync()
    {
        // given — a stop with nothing in it
        ScriptedServerHandler server = ScriptedServerHandler.Answering(
            "{\"content\":[],\"stop_reason\":\"end_turn\"}");

        AnthropicGeneratorBrokerV1 broker = CreateBroker(server);
        var expectedResult = new GenerationResult();

        // when
        GenerationResult actualResult = await broker.GenerateAsync(conversation, tools);

        // then
        actualResult.Should().BeEquivalentTo(expectedResult);
    }
}
