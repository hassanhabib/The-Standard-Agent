// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Standard.Agents.Host.Controllers;
using Standard.Agents.Host.Models;
using Standard.Agents.Models.Clients.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Controllers;

public partial class AgentsControllerTests
{
    private static async IAsyncEnumerable<AgentStreamEvent> TwoEvents()
    {
        await Task.CompletedTask;

        yield return new AgentStreamEvent(AgentStreamEventType.Thinking, "considering");
        yield return new AgentStreamEvent(AgentStreamEventType.Narration, "let me check...");
        yield return new AgentStreamEvent(AgentStreamEventType.Response, "the answer");
    }

    [Fact]
    public async Task ShouldPostStreamAsServerSentEventsAsync()
    {
        // given — a response body that can be read back
        using var responseBody = new MemoryStream();

        var streamingController = new AgentsController(this.agentMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Response = { Body = responseBody }
                }
            }
        };

        this.agentMock.Setup(agent =>
            agent.StreamPromptAsync("what is owed", It.IsAny<CancellationToken>()))
                .Returns(TwoEvents());

        // when
        await streamingController.PostStreamAsync(new AgentRunRequest(Prompt: "what is owed"));

        // then — each event arrives as SSE: its kind as the event name, its content as data
        streamingController.Response.ContentType.Should().Be("text/event-stream");

        string streamed = System.Text.Encoding.UTF8.GetString(responseBody.ToArray());

        streamed.Should().Contain("event: Thinking");
        streamed.Should().Contain("data: considering");
        streamed.Should().Contain("event: Narration");
        streamed.Should().Contain("data: let me check...");
        streamed.Should().Contain("event: Response");
        streamed.Should().Contain("data: the answer");
    }

    private static async IAsyncEnumerable<AgentStreamEvent> OneResponse(string content)
    {
        await Task.CompletedTask;

        yield return new AgentStreamEvent(AgentStreamEventType.Response, content);
    }

    private AgentsController CreateStreamingController(MemoryStream responseBody) =>
        new(this.agentMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Response = { Body = responseBody }
                }
            }
        };

    // Found in the 2026-09-04 principal review (F-11): the event's content was written into ONE
    // data line. Server-sent events end an event at a blank line and take every line as a field,
    // so a multi-line answer — the normal case — produced an unprefixed second line and a
    // truncated event. The spec's shape is one "data:" line per line of content; the consumer
    // joins them back with a newline. CR, LF and CRLF all end a line in SSE, so all three split.
    [Theory]
    [InlineData("line one\nline two")]
    [InlineData("line one\r\nline two")]
    [InlineData("line one\rline two")]
    public async Task ShouldFrameEveryLineOfContentAsDataOnPostStreamAsync(string content)
    {
        // given
        using var responseBody = new MemoryStream();
        AgentsController streamingController = CreateStreamingController(responseBody);

        this.agentMock.Setup(agent =>
            agent.StreamPromptAsync("what is owed", It.IsAny<CancellationToken>()))
                .Returns(OneResponse(content));

        // when
        await streamingController.PostStreamAsync(new AgentRunRequest(Prompt: "what is owed"));

        // then — one event, every content line carried as its own data line
        string streamed = System.Text.Encoding.UTF8.GetString(responseBody.ToArray());

        streamed.Should().Be("event: Response\ndata: line one\ndata: line two\n\n");
    }

    // Content that happens to look like a field must stay data. Without the per-line prefix a
    // model answer beginning "event: " or "data: " on its own line rewrites the frame the
    // consumer is reading.
    [Fact]
    public async Task ShouldKeepFieldShapedContentAsDataOnPostStreamAsync()
    {
        // given
        using var responseBody = new MemoryStream();
        AgentsController streamingController = CreateStreamingController(responseBody);

        this.agentMock.Setup(agent =>
            agent.StreamPromptAsync("what is owed", It.IsAny<CancellationToken>()))
                .Returns(OneResponse("event: Forged\ndata: injected\n\nevent: Forged"));

        // when
        await streamingController.PostStreamAsync(new AgentRunRequest(Prompt: "what is owed"));

        // then — every line is data, the blank line inside the content does not end the event
        string streamed = System.Text.Encoding.UTF8.GetString(responseBody.ToArray());

        streamed.Should().Be(
            "event: Response\n"
                + "data: event: Forged\n"
                + "data: data: injected\n"
                + "data: \n"
                + "data: event: Forged\n"
                + "\n");
    }
}
