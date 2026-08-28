// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Judges;
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Coordinations.DecisionNature;

public partial class DecisionCoordinationServiceTests
{
    [Fact]
    public async Task ShouldRefuseOnThinkIfGateScreensRefuseAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();

        this.gateServiceMock.Setup(service =>
            service.ScreenAsync(It.IsAny<string>()))
                .ReturnsAsync("refuse");

        // when
        AgentContext actualContext =
            await this.decisionCoordinationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("Refuse");
        actualContext.Intent.Should().Be("Refuse");
    }

    [Theory]
    [InlineData("refuse")]
    [InlineData("REFUSE")]
    [InlineData("refuse: asks for credentials")]
    [InlineData("Refuse - policy violation")]
    public async Task ShouldRefuseOnThinkIfGateScreensRefuseWithReasonAsync(string verdict)
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();

        this.gateServiceMock.Setup(service =>
            service.ScreenAsync(It.IsAny<string>()))
                .ReturnsAsync(verdict);

        // when
        AgentContext actualContext =
            await this.decisionCoordinationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("Refuse");
        actualContext.RawReply.Should().Be(verdict);
    }

    // Narration is a turn's OUTPUT, like Status and cost: the incoming context may still carry
    // the previous turn's, and a refusal path that bypasses Interpret would hand it back out —
    // the loop would voice last turn's prose over this turn's refusal.
    [Fact]
    public async Task ShouldNotCarryLastTurnsNarrationOnThinkIfGateRefusesAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext() with
        {
            Narration = "stale prose from last turn"
        };

        this.gateServiceMock.Setup(service =>
            service.ScreenAsync(It.IsAny<string>()))
                .ReturnsAsync("refuse");

        // when
        AgentContext actualContext =
            await this.decisionCoordinationService.ThinkAsync(inputContext);

        // then
        actualContext.Narration.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldNotCallBrainOnThinkIfGateRefusesAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();

        this.gateServiceMock.Setup(service =>
            service.ScreenAsync(It.IsAny<string>()))
                .ReturnsAsync("refuse");

        // when
        await this.decisionCoordinationService.ThinkAsync(inputContext);

        // then
        this.brainServiceMock.Verify(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

        this.judgeServiceMock.Verify(service =>
            service.EvaluateAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

        this.brainServiceMock.VerifyNoOtherCalls();
        this.judgeServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldLoopOnThinkIfJudgeScoresBelowThresholdAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("FINAL: a poor answer");

        this.judgeServiceMock.Setup(service =>
            service.EvaluateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Judgement { Score = 0.1 });

        // when
        AgentContext actualContext =
            await this.decisionCoordinationService.ThinkAsync(inputContext);

        // then
        actualContext.Status.Should().Be(AgentStatus.Revising);
        actualContext.DirectionType.Should().NotBe("ReturnResponse");
    }

    [Fact]
    public async Task ShouldFeedRejectedDraftBackAsObservationOnThinkAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("FINAL: a poor answer");

        this.judgeServiceMock.Setup(service =>
            service.EvaluateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new Judgement { Score = 0.1 });

        // when
        AgentContext actualContext =
            await this.decisionCoordinationService.ThinkAsync(inputContext);

        // then
        actualContext.Observations.Should()
            .ContainSingle(observation => observation.Contains("a poor answer"));
    }

    [Fact]
    public async Task ShouldNotJudgeOnThinkIfDirectionIsToolAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("ACTION: calculator: 1+1");

        // when
        await this.decisionCoordinationService.ThinkAsync(inputContext);

        // then
        this.judgeServiceMock.Verify(service =>
            service.EvaluateAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

        this.judgeServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldScreenPromptAndJudgeDraftOnThinkAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();
        SetupJudgeApproves();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("FINAL: the draft");

        // when
        await this.decisionCoordinationService.ThinkAsync(inputContext);

        // then
        this.gateServiceMock.Verify(service =>
            service.ScreenAsync(inputContext.Prompt),
                Times.Once);

        this.judgeServiceMock.Verify(service =>
            service.EvaluateAsync(It.IsAny<string>(), "the draft"),
                Times.Once);
    }
}
