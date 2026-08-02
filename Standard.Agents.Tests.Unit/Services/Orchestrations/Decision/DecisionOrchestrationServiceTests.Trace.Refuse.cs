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
    public async Task ShouldNarrateGateRefusalReasonOnThinkStreamAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();

        this.gateServiceMock.Setup(service =>
            service.ScreenAsync(It.IsAny<string>()))
                .ReturnsAsync("refuse: asks for personal information");

        // when
        IDecisionStream decisionStream =
            this.decisionOrchestrationService.ThinkStreamAsync(inputContext);

        await DrainAsync(decisionStream);

        // then
        this.loggingBrokerMock.Verify(broker =>
            broker.LogProcessAsync(
                "Decision",
                It.Is<string>(message =>
                    message.Contains("REFUSE")
                        && message.Contains("personal information")),
                It.IsAny<bool>()),
                    Times.Once);
    }
}
