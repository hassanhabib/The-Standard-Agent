// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

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
    public async Task ShouldNarrateProcessesToLoggingBrokerOnThinkStreamAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();
        SetupJudgeApproves();

        this.brainServiceMock.Setup(service =>
            service.GenerateStreamAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(ToAsyncStream("FINAL: 42"));

        // when
        IDecisionStream decisionStream =
            this.decisionCoordinationService.ThinkStreamAsync(inputContext);

        await DrainAsync(decisionStream);

        // then
        this.loggingBrokerMock.Verify(broker =>
            broker.LogProcessAsync(
                "Decision",
                It.Is<string>(message => message.Contains("Gate")),
                It.IsAny<bool>()),
                    Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogProcessAsync(
                "Decision",
                It.Is<string>(message => message.Contains("Judge")),
                It.IsAny<bool>()),
                    Times.Once);
    }
}
