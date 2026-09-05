// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Standard.Agents.Host.Models;
using Standard.Agents.Host.Models.V1;
using Standard.Agents.Models.Brokers.Generators.V1;
using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Effects;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance.Host;

// Found in the 2026-09-04 principal review (F-20): every host test called a controller by hand
// with a DefaultHttpContext, so routing, JSON binding, the API-key middleware, content types
// and the SSE wire could all regress while the suite stayed green. These stand the real host
// up in memory and speak HTTP to it. The agent behind the door is a double: the host is the
// unit here, and the loop has its own suite.
public partial class HostAcceptanceTests : IDisposable
{
    private static readonly JsonSerializerOptions wireOptions =
        new(JsonSerializerDefaults.Web);

    private readonly Mock<IAgent> agentMock;
    private readonly WebApplicationFactory<Program> hostFactory;

    public HostAcceptanceTests()
    {
        this.agentMock = new Mock<IAgent>();

        this.hostFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddSingleton(this.agentMock.Object)));
    }

    public void Dispose() =>
        this.hostFactory.Dispose();

    private static AgentRunRequestV1 CreateFullRequest() =>
        new()
        {
            Prompt = "wire the money",
            SessionId = "trip-3",
            History = [new AgentTurnV1(Prompt: "hello", Answer: "hi")],

            ToolExchanges =
            [
                new ToolExchangeV1(
                    CallId: "call-1",
                    ToolName: "lookup",
                    ArgumentsJson: "{\"account\":\"42\"}",
                    Result: "owed: 12")
            ],

            CallerTools =
            [
                new CallerToolV1(
                    Name: "lookup",
                    Description: "looks up an account",
                    ParametersJson: "{\"type\":\"object\"}")
            ],

            ResponseSchemaJson = "{\"type\":\"object\"}",
            Temperature = 0.2,
            MaxTokens = 256,
            Seed = 7,
            Stop = ["END"],
            ProviderOptionsJson = "{\"thinking\":true}"
        };

    private static PromptRequest CreateExpectedPromptRequest() =>
        new()
        {
            Prompt = "wire the money",
            SessionId = "trip-3",
            History = [new AgentTurn(Prompt: "hello", Answer: "hi")],

            ToolExchanges =
            [
                new ToolExchange(
                    CallId: "call-1",
                    ToolName: "lookup",
                    ArgumentsJson: "{\"account\":\"42\"}",
                    Result: "owed: 12")
            ],

            ResponseSchemaJson = "{\"type\":\"object\"}",
            Temperature = 0.2,
            MaxTokens = 256,
            Seed = 7,
            Stop = ["END"],

            CallerTools =
            [
                new ToolDefinition(
                    Name: "lookup",
                    Description: "looks up an account",
                    ParametersJson: "{\"type\":\"object\"}")
            ],

            ProviderOptionsJson = "{\"thinking\":true}"
        };

    private static AgentEffect CreateHeldEffect() =>
        AgentEffect.For(
            runId: "run-9",
            toolName: "wire",
            arguments: "{\"amount\":100}",
            riskLevel: RiskLevel.Irreversible,
            approvalRequired: true,
            principal: new AgentPrincipal { Id = "hassan", TenantId = "peerllm" },
            scope: "account:42") with
        { CallId = "call-7" };

    [Fact]
    public async Task ShouldRoundTripAHeldRunThroughHttpAsync()
    {
        // given — the full V1 request in, a run held on an authority out
        AgentRunRequestV1 request = CreateFullRequest();
        PromptRequest expectedPromptRequest = CreateExpectedPromptRequest();
        AgentEffect heldEffect = CreateHeldEffect();
        PromptRequest? actualPromptRequest = null;

        this.agentMock.Setup(agent =>
            agent.RunAsync(It.IsAny<PromptRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PromptRequest, CancellationToken>((promptRequest, _) =>
                    actualPromptRequest = promptRequest)
                .ReturnsAsync(new AgentOutcome(
                    Result: "waiting on an authority",
                    Status: AgentStatus.AwaitingApproval,
                    PendingEffect: heldEffect));

        var expectedResponse = new AgentRunResponseV1(
            Result: "waiting on an authority",
            Status: "AwaitingApproval",
            PendingEffect: new PendingEffectV1(
                RunId: "run-9",
                CallId: "call-7",
                ToolName: "wire",
                Arguments: "{\"amount\":100}",
                Scope: "account:42",
                RiskLevel: "Irreversible",
                ApprovalRequired: true,
                IdempotencyKey: heldEffect.IdempotencyKey,
                Principal: "hassan"));

        using HttpClient client = this.hostFactory.CreateClient();

        // when
        using HttpResponseMessage response =
            await client.PostAsJsonAsync("api/V1/agents/runs", request, wireOptions);

        AgentRunResponseV1? actualResponse =
            await response.Content.ReadFromJsonAsync<AgentRunResponseV1>(wireOptions);

        // then — every field survived the JSON wire in both directions, the pending act included
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        actualResponse.Should().BeEquivalentTo(expectedResponse);
        actualPromptRequest.Should().BeEquivalentTo(expectedPromptRequest);
    }

    [Fact]
    public async Task ShouldBindAPromptOnlyDocumentWithEmptyListsThroughHttpAsync()
    {
        // given — the smallest V1 document a caller can post
        PromptRequest? actualPromptRequest = null;

        this.agentMock.Setup(agent =>
            agent.RunAsync(It.IsAny<PromptRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PromptRequest, CancellationToken>((promptRequest, _) =>
                    actualPromptRequest = promptRequest)
                .ReturnsAsync(new AgentOutcome("the answer", AgentStatus.Responded));

        var expectedPromptRequest = new PromptRequest { Prompt = "what is owed" };
        using HttpClient client = this.hostFactory.CreateClient();

        // when
        using HttpResponseMessage response = await client.PostAsync(
            "api/V1/agents/runs",
            new StringContent(
                "{\"prompt\":\"what is owed\"}",
                System.Text.Encoding.UTF8,
                "application/json"));

        // then — an absent list is an empty one, and an absent option is unset
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        actualPromptRequest.Should().BeEquivalentTo(expectedPromptRequest);
    }

    [Fact]
    public async Task ShouldRefuseANullFieldNamingItThroughHttpAsync()
    {
        // given — a list set to null, which the contract does not allow
        using HttpClient client = this.hostFactory.CreateClient();

        // when
        using HttpResponseMessage response = await client.PostAsync(
            "api/V1/agents/runs",
            new StringContent(
                "{\"prompt\":\"what is owed\",\"history\":null}",
                System.Text.Encoding.UTF8,
                "application/json"));

        string body = await response.Content.ReadAsStringAsync();

        // then — 400, naming the field, before any run starts
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("History");

        this.agentMock.Verify(agent =>
            agent.RunAsync(It.IsAny<PromptRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Fact]
    public async Task ShouldRefuseAnEmptyPromptThroughHttpAsync()
    {
        // given
        using HttpClient client = this.hostFactory.CreateClient();

        // when
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/V1/agents/runs",
            new AgentRunRequestV1 { Prompt = "   " },
            wireOptions);

        // then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        this.agentMock.Verify(agent =>
            agent.RunAsync(It.IsAny<PromptRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Fact]
    public async Task ShouldPostAPromptOnlyRunThroughTheConvenienceDoorAsync()
    {
        // given — V0 of the wire, untouched by V1; its controller rides the string door
        this.agentMock.Setup(agent =>
            agent.RunAsync("what is owed", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AgentOutcome("the answer", AgentStatus.Responded));

        var expectedResponse = new AgentRunResponse(Result: "the answer", Status: "Responded");
        using HttpClient client = this.hostFactory.CreateClient();

        // when
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/agents/runs",
            new AgentRunRequest(Prompt: "what is owed"),
            wireOptions);

        AgentRunResponse? actualResponse =
            await response.Content.ReadFromJsonAsync<AgentRunResponse>(wireOptions);

        // then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        actualResponse.Should().BeEquivalentTo(expectedResponse);
    }
}
