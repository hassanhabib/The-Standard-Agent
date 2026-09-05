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
using Standard.Agents.Models.Brokers.Generators.V1;
using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Effects;
using Xunit;

namespace Standard.Agents.Tests.Unit.Controllers.V1;

// Found in the 2026-09-04 principal review (F-10): the host's wire carried a prompt alone and
// answered with a result and a status alone, so sessions, caller transcripts, caller tools,
// approval continuation and pending effects could not cross the HTTP boundary. Version 1 of
// the wire is the whole request and the whole outcome. An exposer is pure mapping over one
// dependency, so these are mapping tests: the wire in, the contract out; the outcome in, the
// wire out; invalid in, 400 out.
public partial class AgentsV1ControllerTests
{
    private readonly Mock<IAgent> agentMock;
    private readonly AgentsV1Controller agentsV1Controller;

    public AgentsV1ControllerTests()
    {
        this.agentMock = new Mock<IAgent>();

        this.agentsV1Controller = new AgentsV1Controller(this.agentMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private static AgentRunRequestV1 CreateFullRequest() =>
        new()
        {
            Prompt = "what is owed",
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
            Prompt = "what is owed",
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

    [Fact]
    public async Task ShouldPostRunAsync()
    {
        // given
        AgentRunRequestV1 request = CreateFullRequest();
        PromptRequest expectedPromptRequest = CreateExpectedPromptRequest();
        PromptRequest? actualPromptRequest = null;

        this.agentMock.Setup(agent =>
            agent.RunAsync(It.IsAny<PromptRequest>(), It.IsAny<CancellationToken>()))
                .Callback<PromptRequest, CancellationToken>((promptRequest, _) =>
                    actualPromptRequest = promptRequest)
                .ReturnsAsync(new AgentOutcome("the answer", AgentStatus.Responded));

        var expectedResponse = new AgentRunResponseV1(
            Result: "the answer",
            Status: "Responded",
            PendingEffect: null);

        // when
        ActionResult<AgentRunResponseV1> actualResult =
            await this.agentsV1Controller.PostRunAsync(request);

        // then — every field of the wire reached the contract, and the outcome reached the wire
        OkObjectResult okResult = actualResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
        actualPromptRequest.Should().BeEquivalentTo(expectedPromptRequest);
    }

    [Fact]
    public async Task ShouldPostRunAndCarryThePendingEffectAsync()
    {
        // given — a run held on an authority, waiting on the caller's tool call
        var request = new AgentRunRequestV1 { Prompt = "wire the money", SessionId = "trip-3" };

        var principal = new AgentPrincipal { Id = "hassan", TenantId = "peerllm" };

        AgentEffect heldEffect = AgentEffect.For(
            runId: "run-9",
            toolName: "wire",
            arguments: "{\"amount\":100}",
            riskLevel: RiskLevel.Irreversible,
            approvalRequired: true,
            principal: principal,
            scope: "account:42") with
        { CallId = "call-7" };

        this.agentMock.Setup(agent =>
            agent.RunAsync(It.IsAny<PromptRequest>(), It.IsAny<CancellationToken>()))
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

        // when
        ActionResult<AgentRunResponseV1> actualResult =
            await this.agentsV1Controller.PostRunAsync(request);

        // then — the act the run is waiting on crossed the wire whole, key and call id included
        OkObjectResult okResult = actualResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task ShouldReturnBadRequestOnPostRunIfPromptIsEmptyAsync()
    {
        // given
        var request = new AgentRunRequestV1 { Prompt = "   ", SessionId = "trip-3" };

        // when
        ActionResult<AgentRunResponseV1> actualResult =
            await this.agentsV1Controller.PostRunAsync(request);

        // then — a prompt of nothing is the caller's mistake, told as 400 before any run starts
        actualResult.Result.Should().BeOfType<BadRequestObjectResult>();

        this.agentMock.Verify(agent =>
            agent.RunAsync(It.IsAny<PromptRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);

        this.agentMock.VerifyNoOtherCalls();
    }
}
