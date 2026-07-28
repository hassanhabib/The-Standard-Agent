// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Orchestrations.Decision;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Decision;

public partial class DecisionOrchestrationServiceTests
{
    [Fact]
    public async Task ShouldStreamAnswerDraftAsThinkingOnThinkStreamAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();
        SetupJudgeApproves();

        this.brainServiceMock.Setup(service =>
            service.GenerateStreamAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(ToAsyncStream("Hello ", "there!"));

        // when
        IDecisionStream decisionStream =
            this.decisionOrchestrationService.ThinkStreamAsync(inputContext);

        List<AgentStreamEvent> actualEvents = await DrainAsync(decisionStream);

        // then
        actualEvents.Should().NotContain(streamEvent =>
            streamEvent.Type == AgentStreamEventType.Response);

        string streamedDraft =
            string.Concat(actualEvents
                .Where(streamEvent => streamEvent.Type == AgentStreamEventType.Thinking)
                .Select(streamEvent => streamEvent.Content));

        streamedDraft.Should().Be("Hello there!");
        decisionStream.Result.DirectionType.Should().Be("ReturnResponse");
        decisionStream.Result.Payload.Should().Be("Hello there!");
    }

    [Fact]
    public async Task ShouldStreamToolReasoningAsThinkingOnThinkStreamAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();

        this.brainServiceMock.Setup(service =>
            service.GenerateStreamAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(ToAsyncStream("ACTION: cal", "culator: 2+2"));

        // when
        IDecisionStream decisionStream =
            this.decisionOrchestrationService.ThinkStreamAsync(inputContext);

        List<AgentStreamEvent> actualEvents = await DrainAsync(decisionStream);

        // then
        actualEvents.Should().Contain(streamEvent =>
            streamEvent.Type == AgentStreamEventType.Thinking);

        actualEvents.Should().NotContain(streamEvent =>
            streamEvent.Type == AgentStreamEventType.Response);

        decisionStream.Result.DirectionType.Should().Be("calculator");
        decisionStream.Result.Payload.Should().Be("2+2");

        this.judgeServiceMock.Verify(service =>
            service.EvaluateAsync(It.IsAny<string>()),
                Times.Never);
    }

    [Fact]
    public async Task ShouldStreamFinalDraftAsThinkingOnThinkStreamAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();
        SetupJudgeApproves();

        this.brainServiceMock.Setup(service =>
            service.GenerateStreamAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(ToAsyncStream("FINAL: ", "The answer is 42."));

        // when
        IDecisionStream decisionStream =
            this.decisionOrchestrationService.ThinkStreamAsync(inputContext);

        List<AgentStreamEvent> actualEvents = await DrainAsync(decisionStream);

        // then
        actualEvents.Should().Contain(streamEvent =>
            streamEvent.Type == AgentStreamEventType.Thinking);

        actualEvents.Should().NotContain(streamEvent =>
            streamEvent.Type == AgentStreamEventType.Response);

        decisionStream.Result.Payload.Should().Be("The answer is 42.");
    }

    [Fact]
    public async Task ShouldNotStreamResponseOnThinkStreamIfGateRefusesAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();

        this.gateServiceMock.Setup(service =>
            service.ScreenAsync(It.IsAny<string>()))
                .ReturnsAsync("refuse: policy");

        // when
        IDecisionStream decisionStream =
            this.decisionOrchestrationService.ThinkStreamAsync(inputContext);

        List<AgentStreamEvent> actualEvents = await DrainAsync(decisionStream);

        // then
        actualEvents.Should().Contain(streamEvent =>
            streamEvent.Type == AgentStreamEventType.Status
                && streamEvent.Content == "gate refused the request");

        actualEvents.Should().NotContain(streamEvent =>
            streamEvent.Type == AgentStreamEventType.Response);

        decisionStream.Result.DirectionType.Should().Be("Refuse");

        this.brainServiceMock.Verify(service =>
            service.GenerateStreamAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Never);
    }

    private static async IAsyncEnumerable<string> ToAsyncStream(params string[] tokens)
    {
        foreach (string token in tokens)
        {
            await Task.Yield();

            yield return token;
        }
    }

    private static async Task<List<AgentStreamEvent>> DrainAsync(IDecisionStream decisionStream)
    {
        List<AgentStreamEvent> events = [];

        await foreach (AgentStreamEvent streamEvent in decisionStream)
        {
            events.Add(streamEvent);
        }

        return events;
    }
}
