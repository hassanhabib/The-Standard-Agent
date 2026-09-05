// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Standard.Agents.Host.Controllers.V1;
using Standard.Agents.Host.Models.V1;
using Standard.Agents.Models.Clients.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Controllers.V1;

public partial class AgentsV1ControllerTests
{
    private static async IAsyncEnumerable<AgentStreamEvent> TwoLineResponse()
    {
        await Task.CompletedTask;

        yield return new AgentStreamEvent(AgentStreamEventType.Thinking, "considering");
        yield return new AgentStreamEvent(AgentStreamEventType.Response, "line one\nline two");
    }

    private AgentsV1Controller CreateStreamingController(MemoryStream responseBody) =>
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

    [Fact]
    public async Task ShouldPostStreamAsServerSentEventsAsync()
    {
        // given — the full request, streamed; a response body that can be read back
        using var responseBody = new MemoryStream();
        AgentsV1Controller streamingController = CreateStreamingController(responseBody);
        AgentRunRequestV1 request = CreateFullRequest();
        PromptRequest expectedPromptRequest = CreateExpectedPromptRequest();
        PromptRequest? actualPromptRequest = null;

        this.agentMock.Setup(agent =>
            agent.StreamPromptAsync(It.IsAny<PromptRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PromptRequest, CancellationToken>((promptRequest, _) =>
                    actualPromptRequest = promptRequest)
                .Returns(TwoLineResponse());

        // when
        await streamingController.PostStreamAsync(request);

        // then — the whole wire reached the contract; each event is one SSE frame, every content
        // line its own data line (F-11 holds on this door too)
        streamingController.Response.ContentType.Should().Be("text/event-stream");
        actualPromptRequest.Should().BeEquivalentTo(expectedPromptRequest);

        string streamed = System.Text.Encoding.UTF8.GetString(responseBody.ToArray());

        streamed.Should().Be(
            "event: Thinking\ndata: considering\n\n"
                + "event: Response\ndata: line one\ndata: line two\n\n");
    }

    [Fact]
    public async Task ShouldReturnBadRequestOnPostStreamIfPromptIsEmptyAsync()
    {
        // given
        using var responseBody = new MemoryStream();
        AgentsV1Controller streamingController = CreateStreamingController(responseBody);
        var request = new AgentRunRequestV1 { Prompt = "", SessionId = "trip-3" };

        // when
        await streamingController.PostStreamAsync(request);

        // then
        streamingController.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        responseBody.Length.Should().Be(0);

        this.agentMock.Verify(agent =>
            agent.StreamPromptAsync(It.IsAny<PromptRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);

        this.agentMock.VerifyNoOtherCalls();
    }
}
