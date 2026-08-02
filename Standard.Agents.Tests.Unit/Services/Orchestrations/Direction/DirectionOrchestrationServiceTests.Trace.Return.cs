// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Moq;
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Direction;

public partial class DirectionOrchestrationServiceTests
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
        await this.directionOrchestrationService.ActAsync(inputContext);

        // then
        this.loggingBrokerMock.Verify(broker =>
            broker.LogProcessAsync(
                "Direction",
                It.Is<string>(message =>
                    message.Contains("returned") && message.Contains("RESULT-MARKER")),
                It.IsAny<bool>()),
                    Times.Once);
    }
}
