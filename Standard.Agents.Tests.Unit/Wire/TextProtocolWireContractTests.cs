// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net;
using System.Text.Json.Nodes;
using FluentAssertions;
using RESTFulSense.Exceptions;
using Standard.Agents.Brokers.Generators;
using Xunit;

namespace Standard.Agents.Tests.Unit.Wire;

// The text-protocol brain over the OpenAI-compatible wire, batched and streamed, read from the
// bytes the real broker sends and parsed from the bytes a server sends back (F-21). The stream
// is where wire contracts drift most: frame boundaries, comments, blank lines, the sentinel.
public class TextProtocolWireContractTests
{
    private const string BaseUrl = "http://provider.test/v1/";

    private static GeneratorBroker CreateBroker(ScriptedServerHandler server) =>
        new(server, BaseUrl, "key-1", "model-1", temperature: 0.2, maxTokens: 64, timeoutSeconds: 30);

    [Fact]
    public async Task ShouldPostAChatCompletionAndReadTheAnswerAsync()
    {
        // given
        ScriptedServerHandler server = ScriptedServerHandler.Answering(
            "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"FINAL: 12\"}}]}");

        GeneratorBroker broker = CreateBroker(server);

        // when
        string actualAnswer = await broker.GenerateAsync("be brief", "what is owed?");

        // then — the route, the credential, and the two-message body
        HttpRequestMessage request = server.Requests.Should().ContainSingle().Subject;
        request.RequestUri.Should().Be(new Uri("http://provider.test/v1/chat/completions"));
        request.Headers.Authorization?.Parameter.Should().Be("key-1");

        JsonNode body = JsonNode.Parse(server.Bodies[0])!;
        body["model"]!.GetValue<string>().Should().Be("model-1");
        body["stream"]?.GetValue<bool>().Should().NotBe(true);

        JsonArray messages = body["messages"]!.AsArray();
        messages[0]!["role"]!.GetValue<string>().Should().Be("system");
        messages[0]!["content"]!.GetValue<string>().Should().Be("be brief");
        messages[1]!["role"]!.GetValue<string>().Should().Be("user");
        messages[1]!["content"]!.GetValue<string>().Should().Be("what is owed?");

        actualAnswer.Should().Be("FINAL: 12");
    }

    [Fact]
    public async Task ShouldStreamDataFramesUntilTheDoneSentinelAsync()
    {
        // given — an SSE body with a comment, a non-data field, blank lines, an empty delta,
        // the sentinel, and a frame after the sentinel that must never be read
        ScriptedServerHandler server = ScriptedServerHandler.Answering(
            ": keep-alive\n"
                + "event: message\n"
                + "data: {\"choices\":[{\"delta\":{\"content\":\"FIN\"}}]}\n\n"
                + "data: {\"choices\":[{\"delta\":{\"content\":\"AL: \"}}]}\n\n"
                + "data: {\"choices\":[{\"delta\":{}}]}\n\n"
                + "data: {\"choices\":[{\"delta\":{\"content\":\"12\"}}]}\n\n"
                + "data: [DONE]\n\n"
                + "data: {\"choices\":[{\"delta\":{\"content\":\"NOT READ\"}}]}\n\n",
            mediaType: "text/event-stream");

        GeneratorBroker broker = CreateBroker(server);
        var streamed = new List<string>();

        // when
        await foreach (string chunk in broker.GenerateStreamAsync("be brief", "what is owed?"))
        {
            streamed.Add(chunk);
        }

        // then — the request asked to stream; only content deltas came out, in order, then stop
        JsonNode body = JsonNode.Parse(server.Bodies[0])!;
        body["stream"]!.GetValue<bool>().Should().BeTrue();
        streamed.Should().Equal("FIN", "AL: ", "12");
    }

    [Fact]
    public async Task ShouldEndTheStreamWhenTheConnectionEndsWithoutASentinelAsync()
    {
        // given — a provider that drops the connection mid-stream
        ScriptedServerHandler server = ScriptedServerHandler.Answering(
            "data: {\"choices\":[{\"delta\":{\"content\":\"FINAL: 1\"}}]}\n\n"
                + "data: {\"choices\":[{\"delta\":{\"content\":\"2\"}}]}\n\n",
            mediaType: "text/event-stream");

        GeneratorBroker broker = CreateBroker(server);
        var streamed = new List<string>();

        // when
        await foreach (string chunk in broker.GenerateStreamAsync("be brief", "what is owed?"))
        {
            streamed.Add(chunk);
        }

        // then — what arrived is delivered; the enumeration completes rather than hangs
        streamed.Should().Equal("FINAL: 1", "2");
    }

    [Fact]
    public async Task ShouldThrowOnAThrottledStreamBeforeReadingAnyFrameAsync()
    {
        // given
        ScriptedServerHandler server = ScriptedServerHandler.AnsweringWith(
            HttpStatusCode.TooManyRequests,
            "{\"error\":{\"message\":\"slow down\"}}");

        GeneratorBroker broker = CreateBroker(server);

        // when
        Func<Task> streamAsync = async () =>
        {
            await foreach (string _ in broker.GenerateStreamAsync("be brief", "what is owed?"))
            {
            }
        };

        // then
        (await streamAsync.Should().ThrowAsync<HttpRequestException>())
            .Which.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ShouldThrowTheTypedRestExceptionOnAThrottledCompletionAsync()
    {
        // given — the batched door rides RESTFulSense, which types the status
        ScriptedServerHandler server = ScriptedServerHandler.AnsweringWith(
            HttpStatusCode.TooManyRequests,
            "{\"error\":{\"message\":\"slow down\"}}");

        GeneratorBroker broker = CreateBroker(server);

        // when
        Func<Task> generateAsync = async () => await broker.GenerateAsync("be brief", "what is owed?");

        // then
        await generateAsync.Should().ThrowAsync<HttpResponseTooManyRequestsException>();
    }
}
