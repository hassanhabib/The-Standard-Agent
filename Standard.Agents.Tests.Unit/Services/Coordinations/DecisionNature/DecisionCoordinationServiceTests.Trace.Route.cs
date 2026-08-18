// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Coordinations.Decision;
using Standard.Agents.Services.Orchestrations.Decision.Guardians;
using Standard.Agents.Services.Orchestrations.Decision.Inferences;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Coordinations.DecisionNature;

public partial class DecisionCoordinationServiceTests
{
    [Fact]
    public async Task ShouldNarrateGateRouteAndProceedOnThinkStreamAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupJudgeApproves();

        this.gateServiceMock.Setup(service =>
            service.ScreenAsync(It.IsAny<string>()))
                .ReturnsAsync("route: arithmetic");

        this.brainServiceMock.Setup(service =>
            service.GenerateStreamAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(ToAsyncStream("FINAL: 42"));

        // when
        IDecisionStream decisionStream =
            this.decisionCoordinationService.ThinkStreamAsync(inputContext);

        await DrainAsync(decisionStream);

        // then a route proceeds to the Brain (not refused) and is narrated as ROUTE
        decisionStream.Result.DirectionType.Should().Be("ReturnResponse");

        this.loggingBrokerMock.Verify(broker =>
            broker.LogProcessAsync(
                "Decision",
                It.Is<string>(message =>
                    message.Contains("ROUTE") && message.Contains("arithmetic")),
                It.IsAny<bool>()),
                    Times.Once);
    }
}
