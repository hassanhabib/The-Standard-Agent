// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Moq;
using Standard.Agents.Host.Models.V1;
using Standard.Agents.Models.Clients.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance.Host;

public partial class HostAcceptanceTests
{
    private static async IAsyncEnumerable<AgentStreamEvent> TwoLineResponse()
    {
        await Task.CompletedTask;

        yield return new AgentStreamEvent(AgentStreamEventType.Thinking, "considering");
        yield return new AgentStreamEvent(AgentStreamEventType.Response, "line one\nline two");
    }

    [Fact]
    public async Task ShouldStreamServerSentEventsThroughHttpAsync()
    {
        // given — the V1 request on the streamed door
        AgentRunRequestV1 request = CreateFullRequest();
        PromptRequest expectedPromptRequest = CreateExpectedPromptRequest();
        PromptRequest? actualPromptRequest = null;

        this.agentMock.Setup(agent =>
            agent.StreamPromptAsync(It.IsAny<PromptRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PromptRequest, CancellationToken>((promptRequest, _) =>
                    actualPromptRequest = promptRequest)
                .Returns(TwoLineResponse());

        using HttpClient client = this.hostFactory.CreateClient();

        // when
        using HttpResponseMessage response =
            await client.PostAsJsonAsync("api/V1/agents/streams", request, wireOptions);

        string streamed = await response.Content.ReadAsStringAsync();

        // then — the SSE content type on the wire, one frame per event, one data line per
        // content line (F-11), and the whole request reached the contract
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");

        streamed.Should().Be(
            "event: Thinking\ndata: considering\n\n"
                + "event: Response\ndata: line one\ndata: line two\n\n");

        actualPromptRequest.Should().BeEquivalentTo(expectedPromptRequest);
    }

    [Fact]
    public async Task ShouldRefuseAnEmptyPromptOnTheStreamedDoorThroughHttpAsync()
    {
        // given
        using HttpClient client = this.hostFactory.CreateClient();

        // when
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/V1/agents/streams",
            new AgentRunRequestV1 { Prompt = "" },
            wireOptions);

        // then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        this.agentMock.Verify(agent =>
            agent.StreamPromptAsync(It.IsAny<PromptRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
    }
}
