// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Decision;

public partial class DecisionOrchestrationServiceTests
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
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

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
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("Refuse");
        actualContext.RawReply.Should().Be(verdict);
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
        await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        this.brainServiceMock.Verify(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

        this.judgeServiceMock.Verify(service =>
            service.EvaluateAsync(It.IsAny<string>()),
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
            service.EvaluateAsync(It.IsAny<string>()))
                .ReturnsAsync(0.1);

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.Status.Should().Be(AgentStatus.Working);
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
            service.EvaluateAsync(It.IsAny<string>()))
                .ReturnsAsync(0.1);

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

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
        await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        this.judgeServiceMock.Verify(service =>
            service.EvaluateAsync(It.IsAny<string>()),
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
        await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        this.gateServiceMock.Verify(service =>
            service.ScreenAsync(inputContext.Prompt),
                Times.Once);

        this.judgeServiceMock.Verify(service =>
            service.EvaluateAsync("the draft"),
                Times.Once);
    }
}
