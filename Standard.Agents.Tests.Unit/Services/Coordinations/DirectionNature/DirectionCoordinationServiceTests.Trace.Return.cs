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
    public async Task ShouldNarrateReturnedResultContentOnActAsync()
    {
        // given
        AgentContext inputContext = new()
        {
            Prompt = "prompt",
            DirectionType = "ReturnResponse",
            Payload = "RESULT-MARKER"
        };

        this.returnServiceMock.Setup(service =>
            service.ReturnAsync("RESULT-MARKER"))
                .ReturnsAsync("RESULT-MARKER");

        // when
        await this.directionCoordinationService.ActAsync(inputContext);

        // then
        this.loggingBrokerMock.Verify(broker =>
            broker.LogPayloadAsync(
                "Direction",
                It.Is<string>(summary => summary.Contains("returned")),
                It.Is<string>(payload => payload.Contains("RESULT-MARKER")),
                It.IsAny<bool>()),
                    Times.Once);
    }
}
