// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Standard.Agents.Host.Models.V1;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance.Host;

public partial class HostAcceptanceTests
{
    // The same host, with the door locked by the one configuration line hosting.md documents.
    private WebApplicationFactory<Program> CreateLockedHost() =>
        this.hostFactory.WithWebHostBuilder(builder =>
            builder.UseSetting("Host:ApiKey", "psk-test"));

    [Fact]
    public async Task ShouldRefuseAgentRoutesWithoutTheApiKeyThroughHttpAsync()
    {
        // given
        using WebApplicationFactory<Program> lockedHost = CreateLockedHost();
        using HttpClient client = lockedHost.CreateClient();

        // when
        using HttpResponseMessage runResponse = await client.PostAsJsonAsync(
            "api/V1/agents/runs",
            new AgentRunRequestV1 { Prompt = "what is owed" },
            wireOptions);

        using HttpResponseMessage heartbeat = await client.GetAsync("api/home");

        // then — the agent route is 401 before any run starts; the heartbeat stays open
        runResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        heartbeat.StatusCode.Should().Be(HttpStatusCode.OK);

        this.agentMock.Verify(agent =>
            agent.RunAsync(It.IsAny<PromptRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Fact]
    public async Task ShouldAdmitAgentRoutesWithTheApiKeyThroughHttpAsync()
    {
        // given
        this.agentMock.Setup(agent =>
            agent.RunAsync(It.IsAny<PromptRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AgentOutcome("the answer", AgentStatus.Responded));

        using WebApplicationFactory<Program> lockedHost = CreateLockedHost();
        using HttpClient client = lockedHost.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "psk-test");

        // when
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/V1/agents/runs",
            new AgentRunRequestV1 { Prompt = "what is owed" },
            wireOptions);

        // then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
