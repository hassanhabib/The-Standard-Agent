// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Decision;

// The conscience half of Think: Gate before the Brain, Judge after it.
public partial class DecisionOrchestrationServiceTests
{
    [Fact]
    public async Task ShouldRefuseOnThinkIfGateScreensRefuseAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();

        this.gateServiceMock.Setup(service =>
            service.ScreenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("refuse");

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("Refuse");
        actualContext.Intent.Should().Be("Refuse");
    }

    // ⚠️ A refusal CARRYING A REASON must still refuse.
    //
    // GateService returns the verdict verbatim, reason and all (#25 pins that). A
    // check of `verdict == "refuse"` would not match "refuse: asks for credentials"
    // — the refusal would be read as an allow and the prompt would reach the Brain.
    // The richer the guardian's answer, the more certainly it would fail open.
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
            service.ScreenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(verdict);

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("Refuse");
        actualContext.RawReply.Should().Be(verdict);
    }

    // Invariant 7.6 in practice: a refused prompt never reaches the Brain at all.
    // Screening that ran but did not gate would be theatre.
    [Fact]
    public async Task ShouldNotCallBrainOnThinkIfGateRefusesAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();

        this.gateServiceMock.Setup(service =>
            service.ScreenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("refuse");

        // when
        await this.decisionOrchestrationService.ThinkAsync(inputContext);

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

    // Invariant 7.5 — a draft is not a commitment. A low-scored answer loops instead
    // of returning, so output does not cross the boundary un-vetted.
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
                .ReturnsAsync(0.1);

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.Status.Should().Be(AgentStatus.Working);
        actualContext.DirectionType.Should().NotBe("ReturnResponse");
    }

    // ⚠️ Theory Ch.8: reflective judgment means "the draft becomes Data, the next
    // Think reconsiders it". The rejected draft itself must reach the next turn — a
    // bare "revise" note tells the Brain it failed but not WHAT failed, so it is
    // just as likely to write the same answer again and loop to the turn cap.
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
                .ReturnsAsync(0.1);

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.Observations.Should()
            .ContainSingle(observation => observation.Contains("a poor answer"));
    }

    // The Judge screens OUTPUT. A tool call is not output — it is a proposal
    // Direction will execute. Judging it would spend an inference call grading
    // something the Judge's rubric was never written for.
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
            service.EvaluateAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

        this.judgeServiceMock.VerifyNoOtherCalls();
    }

    // The Gate screens the PROMPT, and the Judge screens the DRAFT. Passing the wrong
    // text to either would make both guardians look like they ran while grading
    // something no one asked about.
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
            service.ScreenAsync(inputContext.SystemPrompt, inputContext.Prompt),
                Times.Once);

        this.judgeServiceMock.Verify(service =>
            service.EvaluateAsync(inputContext.SystemPrompt, "the draft"),
                Times.Once);
    }
}
