// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Direction;

public partial class DirectionOrchestrationServiceTests
{
        [Fact]
    public async Task ShouldReturnAndTerminateOnActIfDirectionTypeIsReturnResponseAsync()
    {
        // given
        string answer = CreateRandomString();

        AgentContext inputContext =
            CreateContextWithDirection("ReturnResponse", answer);

        this.returnServiceMock.Setup(service =>
            service.ReturnAsync(answer))
                .ReturnsAsync(answer);

        // when
        AgentContext actualContext =
            await this.directionOrchestrationService.ActAsync(inputContext);

        // then
        actualContext.Result.Should().Be(answer);
        actualContext.Status.Should().Be(AgentStatus.Responded);

        this.returnServiceMock.Verify(service =>
            service.ReturnAsync(answer),
                Times.Once);

        this.internalToolServiceMock.VerifyNoOtherCalls();
        this.externalToolServiceMock.VerifyNoOtherCalls();
    }

            [Fact]
    public async Task ShouldRefuseAndTerminateOnActIfDirectionTypeIsRefuseAsync()
    {
        // given
        string refusal = "I'm not able to help with that.";

        AgentContext inputContext =
            CreateContextWithDirection("Refuse", refusal);

        this.returnServiceMock.Setup(service =>
            service.ReturnAsync(refusal))
                .ReturnsAsync(refusal);

        // when
        AgentContext actualContext =
            await this.directionOrchestrationService.ActAsync(inputContext);

        // then
        actualContext.Result.Should().Be(refusal);
        actualContext.Status.Should().Be(AgentStatus.Refused);

        this.internalToolServiceMock.VerifyNoOtherCalls();
        this.externalToolServiceMock.VerifyNoOtherCalls();
    }

            [Fact]
    public async Task ShouldRunInternalToolAndAppendObservationOnActAsync()
    {
        // given
        AgentContext inputContext =
            CreateContextWithDirection("calculator", "1+1");

        this.internalToolServiceMock.Setup(service =>
            service.HandlesAsync("calculator"))
                .ReturnsAsync(true);

        this.internalToolServiceMock.Setup(service =>
            service.RunAsync("calculator", "1+1"))
                .ReturnsAsync("2");

        // when
        AgentContext actualContext =
            await this.directionOrchestrationService.ActAsync(inputContext);

        // then
        actualContext.Status.Should().Be(AgentStatus.Working);
        actualContext.Observations.Should().ContainSingle(o => o.Contains("2"));

        this.externalToolServiceMock.Verify(service =>
            service.CallAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
    }

                    [Fact]
    public async Task ShouldRouteToExternalAndRecoverOnActIfToolIsUnknownAsync()
    {
        // given
        AgentContext inputContext =
            CreateContextWithDirection("weather", "today");

        this.internalToolServiceMock.Setup(service =>
            service.HandlesAsync("weather"))
                .ReturnsAsync(false);

        this.externalToolServiceMock.Setup(service =>
            service.CallAsync("weather", "today"))
                .ReturnsAsync("[external 'weather' not configured]");

        // when
        AgentContext actualContext =
            await this.directionOrchestrationService.ActAsync(inputContext);

        // then
        actualContext.Status.Should().Be(AgentStatus.Working);
        actualContext.Observations.Should().ContainSingle(o => o.Contains("weather"));

        this.externalToolServiceMock.Verify(service =>
            service.CallAsync("weather", "today"),
                Times.Once);

        this.internalToolServiceMock.Verify(service =>
            service.RunAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
    }

            [Fact]
    public async Task ShouldPreserveExistingObservationsOnActAsync()
    {
        // given
        string priorObservation = CreateRandomString();

        AgentContext inputContext = new()
        {
            Prompt = CreateRandomString(),
            DirectionType = "calculator",
            Payload = "1+1",
            Observations = [priorObservation]
        };

        this.internalToolServiceMock.Setup(service =>
            service.HandlesAsync("calculator"))
                .ReturnsAsync(true);

        this.internalToolServiceMock.Setup(service =>
            service.RunAsync("calculator", "1+1"))
                .ReturnsAsync("2");

        // when
        AgentContext actualContext =
            await this.directionOrchestrationService.ActAsync(inputContext);

        // then
        actualContext.Observations.Should().HaveCount(2);
        actualContext.Observations.Should().Contain(priorObservation);
    }

                [Fact]
    public async Task ShouldKeepWorkingOnActIfToolReportsAnErrorAsync()
    {
        // given
        AgentContext inputContext =
            CreateContextWithDirection("calculator", "not math");

        this.internalToolServiceMock.Setup(service =>
            service.HandlesAsync("calculator"))
                .ReturnsAsync(true);

        this.internalToolServiceMock.Setup(service =>
            service.RunAsync("calculator", "not math"))
                .ReturnsAsync("error: could not parse expression");

        // when
        AgentContext actualContext =
            await this.directionOrchestrationService.ActAsync(inputContext);

        // then
        actualContext.Status.Should().Be(AgentStatus.Working);
        actualContext.Observations.Should()
            .ContainSingle(o => o.Contains("could not parse expression"));
    }

                                [Fact]
    public async Task ShouldSetResultToToolOutputOnActEvenIfNonTerminalAsync()
    {
        // given
        AgentContext inputContext =
            CreateContextWithDirection("loop", "x");

        this.internalToolServiceMock.Setup(service =>
            service.HandlesAsync("loop"))
                .ReturnsAsync(true);

        this.internalToolServiceMock.Setup(service =>
            service.RunAsync("loop", "x"))
                .ReturnsAsync("again");

        // when
        AgentContext actualContext =
            await this.directionOrchestrationService.ActAsync(inputContext);

        // then
        actualContext.Result.Should().Be("again");
        actualContext.Status.Should().Be(AgentStatus.Working);
    }

            [Fact]
    public async Task ShouldNameToolInObservationOnActAsync()
    {
        // given
        AgentContext inputContext =
            CreateContextWithDirection("calculator", "1+1");

        this.internalToolServiceMock.Setup(service =>
            service.HandlesAsync("calculator"))
                .ReturnsAsync(true);

        this.internalToolServiceMock.Setup(service =>
            service.RunAsync("calculator", "1+1"))
                .ReturnsAsync("2");

        // when
        AgentContext actualContext =
            await this.directionOrchestrationService.ActAsync(inputContext);

        // then
        actualContext.Observations.Should()
            .ContainSingle(o => o.Contains("calculator") && o.Contains("2"));
    }
}
