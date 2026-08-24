// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Telemetries;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Managements;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Managements;

public partial class RunManagementServiceTests
{
    [Fact]
    public async Task ShouldEmitTelemetryAcrossTheRunAndItsTurnsAsync()
    {
        // given
        string expectedAnswer = CreateRandomString();
        var telemetryBrokerMock = new Mock<ITelemetryBroker>();
        SetupOrchestrationsPassThrough();
        SetupDirectionTerminates(expectedAnswer);

        var telemeteredService = new RunManagementService(
            dataCoordinationService: this.dataCoordinationServiceMock.Object,
            decisionCoordinationService: this.decisionCoordinationServiceMock.Object,
            directionCoordinationService: this.directionCoordinationServiceMock.Object,
            loggingBroker: this.loggingBrokerMock.Object,
            telemetryBroker: telemetryBrokerMock.Object);

        // when
        string actualAnswer = await telemeteredService.ProcessPromptAsync(CreateRandomString());

        // then
        actualAnswer.Should().Be(expectedAnswer);

        telemetryBrokerMock.Verify(broker =>
            broker.StartRun(string.Empty),
                Times.Once);

        telemetryBrokerMock.Verify(broker =>
            broker.StartTurn(0),
                Times.Once);

        telemetryBrokerMock.Verify(broker =>
            broker.RecordTurnUsage(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()),
                Times.Once);

        telemetryBrokerMock.Verify(broker =>
            broker.RecordRunOutcome(
                nameof(AgentStatus.Responded),
                It.IsAny<int>(),
                It.IsAny<int>()),
                    Times.Once);

        telemetryBrokerMock.VerifyNoOtherCalls();
    }
}
