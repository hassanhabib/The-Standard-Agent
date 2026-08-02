// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Orchestrations.Direction;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Direction;

public partial class DirectionOrchestrationServiceTests
{
    [Fact]
    public async Task ShouldDenyToolNotInAllowListOnActAsync()
    {
        // given
        var restrictedService = new DirectionOrchestrationService(
            internalToolService: this.internalToolServiceMock.Object,
            externalToolService: this.externalToolServiceMock.Object,
            returnService: this.returnServiceMock.Object,
            loggingBroker: this.loggingBrokerMock.Object,
            allowedTools: ["calculator"]);

        AgentContext inputContext =
            CreateContextWithDirection("webhook", "https://evil.example/exfiltrate");

        // when
        AgentContext actualContext = await restrictedService.ActAsync(inputContext);

        // then — the forbidden tool never ran; the agent stays working and can recover
        actualContext.Status.Should().Be(AgentStatus.Working);

        actualContext.Observations.Should()
            .ContainSingle(observation => observation.Contains("not permitted"));

        this.internalToolServiceMock.Verify(service =>
            service.HandlesAsync(It.IsAny<string>()), Times.Never);

        this.internalToolServiceMock.Verify(service =>
            service.RunAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        this.externalToolServiceMock.Verify(service =>
            service.CallAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ShouldAllowToolInAllowListOnActAsync()
    {
        // given
        var restrictedService = new DirectionOrchestrationService(
            internalToolService: this.internalToolServiceMock.Object,
            externalToolService: this.externalToolServiceMock.Object,
            returnService: this.returnServiceMock.Object,
            loggingBroker: this.loggingBrokerMock.Object,
            allowedTools: ["calculator"]);

        AgentContext inputContext = CreateContextWithDirection("calculator", "2 + 2");

        this.internalToolServiceMock.Setup(service =>
            service.HandlesAsync("calculator")).ReturnsAsync(true);

        this.internalToolServiceMock.Setup(service =>
            service.RunAsync("calculator", "2 + 2")).ReturnsAsync("4");

        // when
        AgentContext actualContext = await restrictedService.ActAsync(inputContext);

        // then — an allow-listed tool runs normally
        actualContext.Result.Should().Be("4");

        this.internalToolServiceMock.Verify(service =>
            service.RunAsync("calculator", "2 + 2"), Times.Once);
    }
}
