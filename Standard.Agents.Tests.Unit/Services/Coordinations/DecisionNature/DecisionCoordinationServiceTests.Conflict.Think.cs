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
    public async Task ShouldAwaitInputOnThinkIfSkillsConflictAsync()
    {
        // given
        AgentContext inputContext =
            CreateRandomAgentContext() with { SystemPrompt = CreateRandomString() };

        SetupGateAllows();
        SetupJudgeApproves();

        this.gateServiceMock.Setup(service =>
            service.DetectConflictAsync(inputContext.SystemPrompt))
                .ReturnsAsync("CONFLICT: Arabic | answer in Arabic || English | answer in English");

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("FINAL: whatever");

        // when
        AgentContext actualContext =
            await this.decisionCoordinationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("AwaitInput");
        actualContext.Payload.Should().Contain("Arabic");
        actualContext.Payload.Should().Contain("English");

        this.brainServiceMock.Verify(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
    }
}
