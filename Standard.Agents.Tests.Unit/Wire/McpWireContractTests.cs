// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net;
using System.Text.Json.Nodes;
using FluentAssertions;
using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Models.Brokers.Mcps;
using Xunit;

namespace Standard.Agents.Tests.Unit.Wire;

// The MCP JSON-RPC wire, read from the bytes the real broker sends (F-21): tools/list and
// tools/call as the protocol shapes them, ids that increase, schemas that survive untouched,
// arguments that arrive as an object, errors that surface, and a token asked for per request.
public class McpWireContractTests
{
    private static McpBroker CreateBroker(
        ScriptedServerHandler server,
        string? bearerToken = null,
        string? apiKey = null,
        Func<ValueTask<string>>? bearerTokenProvider = null) =>
        new(
            server,
            endpointUrl: "http://mcp.test/",
            relativeUrl: "rpc",
            timeoutSeconds: 30,
            bearerToken,
            apiKey,
            apiKeyHeader: "X-Api-Key",
            bearerTokenProvider);

    [Fact]
    public async Task ShouldPostToolsListAsJsonRpcAndKeepSchemasWholeAsync()
    {
        // given — a server listing one tool with a typed schema
        ScriptedServerHandler server = ScriptedServerHandler.Answering(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"tools\":[{\"name\":\"lookup\","
                + "\"description\":\"looks up an account\","
                + "\"inputSchema\":{\"type\":\"object\",\"properties\":{\"account\":{\"type\":\"string\"}},"
                + "\"required\":[\"account\"]}}]}}");

        McpBroker broker = CreateBroker(server, apiKey: "mcp-key");

        var expectedTools = new List<McpTool>
        {
            new(
                Name: "lookup",
                Description: "looks up an account",
                InputSchemaJson:
                    "{\"type\":\"object\",\"properties\":{\"account\":{\"type\":\"string\"}},"
                        + "\"required\":[\"account\"]}")
        };

        // when
        IReadOnlyList<McpTool> actualTools = await broker.ListToolsAsync();

        // then — the route, the credential header, the envelope
        HttpRequestMessage request = server.Requests.Should().ContainSingle().Subject;
        request.RequestUri.Should().Be(new Uri("http://mcp.test/rpc"));
        request.Headers.GetValues("X-Api-Key").Should().ContainSingle("mcp-key");

        JsonNode body = JsonNode.Parse(server.Bodies[0])!;
        body["jsonrpc"]!.GetValue<string>().Should().Be("2.0");
        body["method"]!.GetValue<string>().Should().Be("tools/list");
        body["id"]!.GetValue<int>().Should().Be(1);

        // and the schema, verbatim (F-03)
        actualTools.Should().BeEquivalentTo(expectedTools);
    }

    [Fact]
    public async Task ShouldPostToolsCallWithArgumentsAsAnObjectAndIncreasingIdsAsync()
    {
        // given — a call after a list on the same broker
        var server = new ScriptedServerHandler((_, body) =>
            body.Contains("tools/list")
                ? ScriptedServerHandler.Json(
                    HttpStatusCode.OK,
                    "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"tools\":[]}}")
                : ScriptedServerHandler.Json(
                    HttpStatusCode.OK,
                    "{\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"content\":"
                        + "[{\"type\":\"text\",\"text\":\"owed: \"},{\"type\":\"text\",\"text\":\"12\"}]}}"));

        McpBroker broker = CreateBroker(server, bearerToken: "static-token");

        // when
        await broker.ListToolsAsync();
        string actualText = await broker.CallAsync("lookup", "{\"account\":\"42\",\"deep\":{\"n\":1}}");

        // then — params as the protocol reads them, id advanced, text blocks joined
        JsonNode call = JsonNode.Parse(server.Bodies[1])!;
        call["method"]!.GetValue<string>().Should().Be("tools/call");
        call["id"]!.GetValue<int>().Should().Be(2);
        call["params"]!["name"]!.GetValue<string>().Should().Be("lookup");
        call["params"]!["arguments"]!["account"]!.GetValue<string>().Should().Be("42");
        call["params"]!["arguments"]!["deep"]!["n"]!.GetValue<int>().Should().Be(1);

        server.Requests[1].Headers.Authorization?.Scheme.Should().Be("Bearer");
        server.Requests[1].Headers.Authorization?.Parameter.Should().Be("static-token");
        actualText.Should().Be("owed: 12");
    }

    [Fact]
    public async Task ShouldThrowHttpRequestExceptionOnAJsonRpcErrorAsync()
    {
        // given — a well-formed 200 carrying a protocol error
        ScriptedServerHandler server = ScriptedServerHandler.Answering(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"error\":{\"code\":-32601,\"message\":\"unknown tool\"}}");

        McpBroker broker = CreateBroker(server);

        // when
        Func<Task> callAsync = async () => await broker.CallAsync("nope", "{}");

        // then — the server's own words, as a native exception for the foundation to localize
        (await callAsync.Should().ThrowAsync<HttpRequestException>())
            .WithMessage("unknown tool");
    }

    [Fact]
    public async Task ShouldAskTheTokenProviderOnEveryRequestAsync()
    {
        // given — an access token that changes between calls, as an OAuth token does
        ScriptedServerHandler server = ScriptedServerHandler.Answering(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"tools\":[]}}");

        int issued = 0;

        McpBroker broker = CreateBroker(
            server,
            bearerToken: "stale-static-token",
            bearerTokenProvider: async () => $"token-{++issued}");

        // when
        await broker.ListToolsAsync();
        await broker.ListToolsAsync();

        // then — the provider wins over the static token, and every request asked again
        server.Requests[0].Headers.Authorization?.Parameter.Should().Be("token-1");
        server.Requests[1].Headers.Authorization?.Parameter.Should().Be("token-2");
    }
}
