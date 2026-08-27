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
    // Found in the 2026-08-23 sweep: the route label was captured on the streamed door only —
    // the mirror image of every other asymmetry, and the same defect. Data consumes
    // context.Route on both paths (RetrieveInstructionsAsync), so a Gate that answered
    // "route: X" steered skill selection when the caller streamed and was silently ignored
    // when the caller did not.
    [Fact]
    public async Task ShouldCaptureGateRouteLabelAsDataOnThinkAsync()
    {
        // given — the identical verdict the streamed test below uses
        AgentContext inputContext = CreateRandomAgentContext();
        SetupJudgeApproves();

        this.gateServiceMock.Setup(service =>
            service.ScreenAsync(It.IsAny<string>()))
                .ReturnsAsync("route: arithmetic");

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("FINAL: 42");

        // when
        AgentContext actualContext =
            await this.decisionCoordinationService.ThinkAsync(inputContext);

        // then — the label rides through as Data on this door too
        actualContext.Route.Should().Be("arithmetic");
    }

    [Fact]
    public async Task ShouldCaptureGateRouteLabelAsDataOnThinkStreamAsync()
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

        // then — the label rode through as Data on the resulting context
        decisionStream.Result.Route.Should().Be("arithmetic");
    }
}
