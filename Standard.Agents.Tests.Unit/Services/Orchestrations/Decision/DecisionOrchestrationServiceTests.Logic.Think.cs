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
    public async Task ShouldThinkAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();
        SetupJudgeApproves();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("FINAL: 42");

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("ReturnResponse");
        actualContext.Payload.Should().Be("42");
        actualContext.RawReply.Should().Be("FINAL: 42");
    }

    [Fact]
    public async Task ShouldParseActionFromFirstLineOnlyOnThinkAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("ACTION: calculator: 1+1\nFINAL: wrong");

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("calculator");
        actualContext.Payload.Should().Be("1+1");

        actualContext.Payload.Should().NotContain("wrong");
    }

    [Fact]
    public async Task ShouldPreserveMultilineFinalOnThinkAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();
        SetupJudgeApproves();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("FINAL: line one\nline two");

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("ReturnResponse");
        actualContext.Payload.Should().Be("line one\nline two");
    }

    [Fact]
    public async Task ShouldPreserveColonsInToolInputOnThinkAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("ACTION: fetch: https://example.com:8080/a?b=1");

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("fetch");
        actualContext.Payload.Should().Be("https://example.com:8080/a?b=1");
    }

    [Fact]
    public async Task ShouldSetIntentToToolNameOnThinkIfActionAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("ACTION: calculator: 1+1");

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.Intent.Should().Be("calculator");
    }

    [Fact]
    public async Task ShouldSetIntentToRespondOnThinkIfFinalAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();
        SetupJudgeApproves();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("FINAL: 42");

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.Intent.Should().Be("Respond");
    }

    [Fact]
    public async Task ShouldTreatNonProtocolReplyAsAnswerOnThinkAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();
        SetupJudgeApproves();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("just an answer, no prefix");

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("ReturnResponse");
        actualContext.Payload.Should().Be("just an answer, no prefix");
    }

    [Theory]
    [InlineData("ACTION:")]
    [InlineData("ACTION: ")]
    [InlineData("ACTION: : missing tool name")]
    public async Task ShouldTreatEmptyActionAsAnswerOnThinkAsync(string reply)
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();
        SetupJudgeApproves();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(reply);

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("ReturnResponse");
        actualContext.Intent.Should().Be("Respond");
    }

    [Fact]
    public async Task ShouldPassObservationsToBrainOnThinkAsync()
    {
        // given
        string observation = CreateRandomString();

        AgentContext inputContext = new()
        {
            Prompt = CreateRandomString(),
            SystemPrompt = CreateRandomString(),
            Observations = [observation]
        };

        SetupGateAllows();
        SetupJudgeApproves();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("FINAL: done");

        // when
        await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        this.brainServiceMock.Verify(service =>
            service.GenerateAsync(
                inputContext.SystemPrompt,
                It.Is<string>(userMessage => userMessage.Contains(observation))),
                    Times.Once);
    }

    [Fact]
    public async Task ShouldRouteStructuredToolCallOnThinkAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(
                    "TOOL: {\"tool\":\"calculator\",\"arguments\":{\"expression\":\"47*89\"}}");

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("calculator");
        actualContext.Intent.Should().Be("calculator");
        actualContext.Payload.Should().Contain("47*89");
    }

    [Fact]
    public async Task ShouldTreatMalformedToolCallAsAnswerOnThinkAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();
        SetupJudgeApproves();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("TOOL: this is not valid json");

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("ReturnResponse");
    }
}
