// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Judges;
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Decision;

public partial class DecisionOrchestrationServiceTests
{
    [Fact]
    public async Task ShouldFeedJudgeReasonBackAsRevisionFeedbackOnThinkAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("FINAL: a poor answer");

        this.judgeServiceMock.Setup(service =>
            service.EvaluateAsync(It.IsAny<string>()))
                .ReturnsAsync(new Judgement
                {
                    Score = 0.1,
                    Reason = "it never states the year"
                });

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.Status.Should().Be(AgentStatus.Revising);

        actualContext.Observations.Should()
            .ContainSingle(observation =>
                observation.Contains("it never states the year"));
    }
}
