// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Moq;
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Coordinations.DirectionNature;

public partial class DirectionCoordinationServiceTests
{
    [Fact]
    public async Task ShouldNarrateProcessesToLoggingBrokerOnActAsync()
    {
        // given
        AgentContext inputContext = new()
        {
            Prompt = "prompt",
            DirectionType = "calculator",
            Payload = "1+1"
        };

        this.internalToolServiceMock.Setup(service =>
            service.HandlesAsync("calculator"))
                .ReturnsAsync(true);

        this.internalToolServiceMock.Setup(service =>
            service.RunAsync("calculator", "1+1"))
                .ReturnsAsync("2");

        // when
        await this.directionCoordinationService.ActAsync(inputContext);

        // then
        this.loggingBrokerMock.Verify(broker =>
            broker.LogPayloadAsync(
                "Direction",
                It.Is<string>(summary => summary.Contains("Tool")),
                It.Is<string>(payload => payload.Contains("2")),
                It.IsAny<bool>()),
                    Times.Once);
    }
}
