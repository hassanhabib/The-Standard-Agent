// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json.Nodes;
using FluentAssertions;
using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Models.Brokers.Mcps;
using Xunit;

namespace Standard.Agents.Tests.Unit.Wire;

// The MCP stdio wire, read from the lines the real broker writes: one JSON-RPC message per line,
// the lifecycle first, notifications skipped, a server's own requests answered.
public class StdioMcpWireContractTests
{
    private const string InitializeResult =
        "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"protocolVersion\":\"2025-06-18\","
            + "\"capabilities\":{\"tools\":{}},\"serverInfo\":{\"name\":\"students\",\"version\":\"1.0.0\"}}}";

    private static IEnumerable<string> Answer(JsonNode message, params string[] lines) =>
        lines.Select(line => line.Replace("{id}", message["id"]!.ToJsonString()));

    [Fact]
    public async Task ShouldInitializeOverStandardInputBeforeTheFirstRequestAsync()
    {
        // given — a server that expects the lifecycle before anything else
        var server = new ScriptedStdioServer(message =>
            message["method"]!.GetValue<string>() switch
            {
                "initialize" => Answer(message, InitializeResult.Replace("\"id\":1", "\"id\":{id}")),
                "notifications/initialized" => [],
                _ => Answer(message,
                    "{\"jsonrpc\":\"2.0\",\"id\":{id},\"result\":{\"tools\":[{\"name\":\"find_student\","
                        + "\"description\":\"Finds a student by their id.\","
                        + "\"inputSchema\":{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"integer\"}}}}]}}")
            });

        var broker = new StdioMcpBroker(server.Output, server.Input, timeoutSeconds: 30);

        var expectedTools = new List<McpTool>
        {
            new(
                Name: "find_student",
                Description: "Finds a student by their id.",
                InputSchemaJson: "{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"integer\"}}}")
        };

        // when
        IReadOnlyList<McpTool> actualTools = await broker.ListToolsAsync();

        // then — initialize, the initialized notification, then the listing; one message per line
        server.Lines.Select(line => JsonNode.Parse(line)!["method"]!.GetValue<string>()).Should().Equal(
            "initialize",
            "notifications/initialized",
            "tools/list");

        server.Lines.Should().AllSatisfy(line => line.Should().NotContain("\n"));
        actualTools.Should().BeEquivalentTo(expectedTools);
    }

    [Fact]
    public async Task ShouldSkipWhatIsNotItsAnswerAndAnswerThePingsOfTheServerAsync()
    {
        // given — a server that, before answering, logs a line to its output, sends a
        // notification, and pings the client with an id that collides with the client's own
        var server = new ScriptedStdioServer(message =>
            message["method"]?.GetValue<string>() switch
            {
                "initialize" => Answer(message, InitializeResult.Replace("\"id\":1", "\"id\":{id}")),
                "tools/list" => Answer(message,
                    "server ready on stdio",
                    "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/message\",\"params\":{\"level\":\"info\"}}",
                    "{\"jsonrpc\":\"2.0\",\"id\":{id},\"method\":\"ping\"}",
                    "{\"jsonrpc\":\"2.0\",\"id\":{id},\"result\":{\"tools\":[{\"name\":\"find_student\"}]}}"),
                _ => []
            });

        var broker = new StdioMcpBroker(server.Output, server.Input, timeoutSeconds: 30);

        // when
        IReadOnlyList<McpTool> actualTools = await broker.ListToolsAsync();

        // then — the answer is found past the noise, and the ping was answered
        actualTools.Should().ContainSingle(tool => tool.Name == "find_student");

        JsonNode pong = JsonNode.Parse(server.Lines[^1])!;
        pong["id"]!.GetValue<int>().Should().Be(2);
        pong["result"].Should().BeOfType<JsonObject>();
        pong["method"].Should().BeNull();
    }
}
