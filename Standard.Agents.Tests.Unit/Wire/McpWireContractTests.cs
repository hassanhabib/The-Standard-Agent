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

        // then — the route, the credential header, the envelope, after the handshake
        HttpRequestMessage request = server.Requests[^1];
        request.RequestUri.Should().Be(new Uri("http://mcp.test/rpc"));
        request.Headers.GetValues("X-Api-Key").Should().ContainSingle("mcp-key");

        JsonNode body = JsonNode.Parse(server.Bodies[^1])!;
        body["jsonrpc"]!.GetValue<string>().Should().Be("2.0");
        body["method"]!.GetValue<string>().Should().Be("tools/list");
        body["id"]!.GetValue<int>().Should().Be(2);

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
        JsonNode call = JsonNode.Parse(server.Bodies[^1])!;
        call["method"]!.GetValue<string>().Should().Be("tools/call");
        call["id"]!.GetValue<int>().Should().Be(3);
        call["params"]!["name"]!.GetValue<string>().Should().Be("lookup");
        call["params"]!["arguments"]!["account"]!.GetValue<string>().Should().Be("42");
        call["params"]!["arguments"]!["deep"]!["n"]!.GetValue<int>().Should().Be(1);

        server.Requests[^1].Headers.Authorization?.Scheme.Should().Be("Bearer");
        server.Requests[^1].Headers.Authorization?.Parameter.Should().Be("static-token");
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
    public async Task ShouldAcceptJsonAndAnEventStreamOnEveryRequestAsync()
    {
        // given — a server free to answer either way, as the Streamable HTTP transport allows
        ScriptedServerHandler server = ScriptedServerHandler.Answering(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"tools\":[]}}");

        McpBroker broker = CreateBroker(server);

        // when
        await broker.ListToolsAsync();

        // then — every request says it reads both
        server.Requests.Should().AllSatisfy(request =>
            request.Headers.Accept.Select(accept => accept.MediaType)
                .Should().Contain(["application/json", "text/event-stream"]));
    }

    [Fact]
    public async Task ShouldReadTheReplyFromAnEventStreamAsync()
    {
        // given — a server answering as the official SDKs do: an event stream, where a
        // notification may arrive before the response it carries
        ScriptedServerHandler server = ScriptedServerHandler.Answering(
            "event: message\n"
                + "data: {\"jsonrpc\":\"2.0\",\"method\":\"notifications/message\","
                + "\"params\":{\"level\":\"info\",\"data\":\"listing\"}}\n"
                + "\n"
                + "event: message\n"
                + "data: {\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"tools\":[{\"name\":\"find_student\","
                + "\"description\":\"Finds a student by their id.\","
                + "\"inputSchema\":{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"integer\"}}}}]}}\n"
                + "\n",
            mediaType: "text/event-stream");

        McpBroker broker = CreateBroker(server);

        var expectedTools = new List<McpTool>
        {
            new(
                Name: "find_student",
                Description: "Finds a student by their id.",
                InputSchemaJson: "{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"integer\"}}}")
        };

        // when
        IReadOnlyList<McpTool> actualTools = await broker.ListToolsAsync();

        // then — the response is found past the notification, the schema whole
        actualTools.Should().BeEquivalentTo(expectedTools);
    }

    [Fact]
    public async Task ShouldInitializeOnceBeforeTheFirstRequestAsync()
    {
        // given — a server that expects the lifecycle the protocol defines
        var server = new ScriptedServerHandler((_, body) =>
            JsonNode.Parse(body)!["method"]!.GetValue<string>() switch
            {
                "initialize" => ScriptedServerHandler.Json(
                    HttpStatusCode.OK,
                    "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"protocolVersion\":\"2025-06-18\","
                        + "\"capabilities\":{\"tools\":{}},\"serverInfo\":{\"name\":\"students\",\"version\":\"1.0.0\"}}}"),

                "notifications/initialized" => new HttpResponseMessage(HttpStatusCode.Accepted),

                _ => ScriptedServerHandler.Json(
                    HttpStatusCode.OK,
                    "{\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"tools\":[]}}")
            });

        McpBroker broker = CreateBroker(server);

        // when
        await broker.ListToolsAsync();
        await broker.ListToolsAsync();

        // then — initialize, then the initialized notification, then the work; once only
        List<JsonNode> messages = [.. server.Bodies.Select(body => JsonNode.Parse(body)!)];

        messages.Select(message => message["method"]!.GetValue<string>()).Should().Equal(
            "initialize",
            "notifications/initialized",
            "tools/list",
            "tools/list");

        JsonNode initialize = messages[0];
        initialize["id"].Should().NotBeNull();
        initialize["params"]!["protocolVersion"]!.GetValue<string>().Should().Be("2025-06-18");
        initialize["params"]!["capabilities"].Should().BeOfType<JsonObject>();
        initialize["params"]!["clientInfo"]!["name"]!.GetValue<string>().Should().Be("Standard.Agents");
        messages[1]["id"].Should().BeNull();
    }

    [Fact]
    public async Task ShouldCarryTheSessionAndTheNegotiatedVersionAfterInitializeAsync()
    {
        // given — a server that opens a session and settles on an older protocol version
        var server = new ScriptedServerHandler((_, body) =>
        {
            if (body.Contains("\"initialize\""))
            {
                HttpResponseMessage initializeResponse = ScriptedServerHandler.Json(
                    HttpStatusCode.OK,
                    "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"protocolVersion\":\"2025-03-26\","
                        + "\"capabilities\":{\"tools\":{}},\"serverInfo\":{\"name\":\"s\",\"version\":\"1\"}}}");

                initializeResponse.Headers.Add("Mcp-Session-Id", "session-7");

                return initializeResponse;
            }

            return ScriptedServerHandler.Json(
                HttpStatusCode.OK,
                "{\"jsonrpc\":\"2.0\",\"id\":3,\"result\":{\"tools\":[]}}");
        });

        McpBroker broker = CreateBroker(server);

        // when
        await broker.ListToolsAsync();

        // then — initialize opens the session; everything after it carries the session
        // and the version the server chose
        server.Requests[0].Headers.Contains("Mcp-Session-Id").Should().BeFalse();

        server.Requests.Skip(1).Should().AllSatisfy(request =>
        {
            request.Headers.GetValues("Mcp-Session-Id").Should().ContainSingle("session-7");
            request.Headers.GetValues("MCP-Protocol-Version").Should().ContainSingle("2025-03-26");
        });
    }

    [Fact]
    public async Task ShouldStartANewSessionWhenTheServerForgetsTheOldOneAsync()
    {
        // given — a server that ends its first session after one listing, answering 404 to it
        // from then on, as the protocol says a server that terminated a session does
        int openedSessions = 0;
        int listings = 0;

        var server = new ScriptedServerHandler((request, body) =>
        {
            if (body.Contains("\"initialize\""))
            {
                HttpResponseMessage initializeResponse = ScriptedServerHandler.Json(
                    HttpStatusCode.OK,
                    "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"protocolVersion\":\"2025-06-18\","
                        + "\"capabilities\":{},\"serverInfo\":{\"name\":\"s\",\"version\":\"1\"}}}");

                initializeResponse.Headers.Add("Mcp-Session-Id", $"session-{++openedSessions}");

                return initializeResponse;
            }

            bool isFirstSession =
                request.Headers.TryGetValues("Mcp-Session-Id", out IEnumerable<string>? sessionIds)
                    && sessionIds.Single() == "session-1";

            if (body.Contains("tools/list") && isFirstSession && listings++ > 0)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return ScriptedServerHandler.Json(
                HttpStatusCode.OK,
                "{\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"tools\":[{\"name\":\"lookup\"}]}}");
        });

        McpBroker broker = CreateBroker(server);
        await broker.ListToolsAsync();

        // when
        IReadOnlyList<McpTool> actualTools = await broker.ListToolsAsync();

        // then — a fresh initialize, without the dead session, and the listing retried on the new one
        actualTools.Should().ContainSingle(tool => tool.Name == "lookup");
        openedSessions.Should().Be(2);

        HttpRequestMessage reinitialize = server.Requests[4];
        server.Bodies[4].Should().Contain("\"initialize\"");
        reinitialize.Headers.Contains("Mcp-Session-Id").Should().BeFalse();

        server.Requests[^1].Headers.GetValues("Mcp-Session-Id").Should().ContainSingle("session-2");
    }

    [Fact]
    public async Task ShouldCarryOnWithoutASessionWhenTheServerHasNoInitializeAsync()
    {
        // given — a server older than the lifecycle: it knows its tools, not initialize
        var server = new ScriptedServerHandler((_, body) =>
            body.Contains("\"initialize\"")
                ? ScriptedServerHandler.Json(
                    HttpStatusCode.OK,
                    "{\"jsonrpc\":\"2.0\",\"id\":1,\"error\":{\"code\":-32601,\"message\":\"Method not found\"}}")
                : ScriptedServerHandler.Json(
                    HttpStatusCode.OK,
                    "{\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"tools\":[{\"name\":\"lookup\"}]}}"));

        McpBroker broker = CreateBroker(server);

        // when
        IReadOnlyList<McpTool> actualTools = await broker.ListToolsAsync();

        // then — its tools still reach the agent, and nothing announces a session that never opened
        actualTools.Should().ContainSingle(tool => tool.Name == "lookup");
        server.Bodies.Should().NotContain(body => body.Contains("notifications/initialized"));
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
