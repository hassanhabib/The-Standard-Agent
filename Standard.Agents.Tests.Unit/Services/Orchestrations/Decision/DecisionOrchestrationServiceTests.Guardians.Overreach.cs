// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Moq;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Orchestrations.Decision;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Decision;

public partial class DecisionOrchestrationServiceTests
{
    [Fact]
    public async Task ShouldNarrateGuardianOverreachWhenGateActsLikeBrainAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();

        this.gateServiceMock.Setup(service =>
            service.ScreenAsync(It.IsAny<string>()))
                .ReturnsAsync("FINAL: Paris");

        SetupJudgeApproves();

        this.brainServiceMock.Setup(service =>
            service.GenerateStreamAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(ToAsyncStream("FINAL: Paris"));

        // when — the Gate tries to answer instead of classifying
        IDecisionStream decisionStream =
            this.decisionOrchestrationService.ThinkStreamAsync(inputContext);

        await DrainAsync(decisionStream);

        // then — the overreach is neutralized and recorded (Invariant 6)
        this.loggingBrokerMock.Verify(broker =>
            broker.LogProcessAsync(
                "Decision",
                It.Is<string>(message => message.Contains("Invariant 6")),
                It.IsAny<bool>()),
                    Times.Once);
    }
}
