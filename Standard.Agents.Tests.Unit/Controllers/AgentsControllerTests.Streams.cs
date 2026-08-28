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
}
