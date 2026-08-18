// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Coordinations.DecisionNature;

public partial class DecisionCoordinationServiceTests
{
    [Fact]
    public async Task ShouldAutoResolveSkillConflictFromLearnedPreferenceOnThinkAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext() with
        {
            SystemPrompt = CreateRandomString(),
            Observations = ["SKILL_PREFERENCE::arabic|english::Arabic"]
        };

        SetupGateAllows();
        SetupJudgeApproves();

        this.gateServiceMock.Setup(service =>
            service.DetectConflictAsync(It.IsAny<string>()))
                .ReturnsAsync("CONFLICT: Arabic | answer in Arabic || English | answer in English");

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("FINAL: an answer");

        // when
        AgentContext actualContext =
            await this.decisionCoordinationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("ReturnResponse");

        this.brainServiceMock.Verify(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
    }

    [Fact]
    public async Task ShouldLearnPreferenceOnThinkIfPromptAnswersSkillConflictAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext() with
        {
            Prompt = "Arabic",
            SystemPrompt = CreateRandomString()
        };

        SetupGateAllows();

        this.gateServiceMock.Setup(service =>
            service.DetectConflictAsync(It.IsAny<string>()))
                .ReturnsAsync("CONFLICT: Arabic | answer in Arabic || English | answer in English");

        // when
        AgentContext actualContext =
            await this.decisionCoordinationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("ReturnResponse");
        actualContext.Payload.Should().Contain("Arabic");
        actualContext.Remember.Should().StartWith("SKILL_PREFERENCE::");
        actualContext.Remember.Should().EndWith("Arabic");

        this.brainServiceMock.Verify(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
    }
}
