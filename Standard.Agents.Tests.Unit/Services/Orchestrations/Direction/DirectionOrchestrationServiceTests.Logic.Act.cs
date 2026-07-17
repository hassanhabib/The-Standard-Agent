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
    // ReturnResponse is terminal -> Responded. SPEC.md 6.
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

    // Refuse is terminal -> Refused. SPEC.md 6. It still goes through ReturnService,
    // because a refusal IS the agent's answer — the caller gets told, not stonewalled.
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

    // Vector 02. A known tool runs Internal, and its result MUST become an
    // observation with status Working — that feed-back is how the next Think sees it.
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

    // Vector 05. An unknown tool routes to EXTERNAL and the agent recovers — it does
    // not throw and does not terminate. Anything the agent cannot do locally might
    // still be doable across the boundary, so a miss is a routing decision, not a
    // failure.
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

    // Observations accumulate. Direction appends its result to what is already there
    // rather than replacing it — the turn history is the Brain's working memory.
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

    // A tool that RETURNS an error string is a result, not a failure. It becomes an
    // observation and the loop keeps going — the Brain gets to read the error and
    // try something else. Only a THROWING tool is a dependency failure.
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

    // ⚠️ Result is written on EVERY turn, not only terminal ones. SPEC.md 3 says
    // "result: written by Act" with no terminal-only qualifier.
    //
    // Vector 06 is what needs this: a Brain that never terminates is capped by the
    // loop, and SPEC.md 5 then returns context.result. If Act only wrote Result on a
    // terminal turn, a capped loop would return "" — the vector expects the last
    // tool output. Result always holds the most recent thing Direction produced.
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

    // The observation names the tool, not just its output. With several tools in
    // flight, a bare "2" tells the Brain nothing about which question it answers.
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
