// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Coordinations;

public partial class AgentCoordinationServiceTests
{
    [Fact]
    public async Task ShouldProcessPromptAsync()
    {
        // given
        string randomPrompt = CreateRandomString();
        string expectedResult = CreateRandomString();

        SetupOrchestrationsPassThrough();
        SetupDirectionTerminates(expectedResult);

        // when
        string actualResult =
            await this.agentCoordinationService.ProcessPromptAsync(randomPrompt);

        // then
        actualResult.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ShouldSeedContextWithPromptOnProcessPromptAsync()
    {
        // given
        string randomPrompt = CreateRandomString();

        SetupOrchestrationsPassThrough();
        SetupDirectionTerminates(CreateRandomString());

        // when
        await this.agentCoordinationService.ProcessPromptAsync(randomPrompt);

        // then
        this.dataOrchestrationServiceMock.Verify(service =>
            service.RecallAsync(It.Is<AgentContext>(context =>
                context.Prompt == randomPrompt
                    && context.Status == AgentStatus.Working)),
                        Times.Once);
    }

    [Fact]
    public async Task ShouldCallRecallThinkActInOrderOnProcessPromptAsync()
    {
        // given
        string randomPrompt = CreateRandomString();
        var sequence = new MockSequence();

        this.dataOrchestrationServiceMock.InSequence(sequence)
            .Setup(service => service.RecallAsync(It.IsAny<AgentContext>()))
            .ReturnsAsync((AgentContext context) => context);

        this.decisionOrchestrationServiceMock.InSequence(sequence)
            .Setup(service => service.ThinkAsync(It.IsAny<AgentContext>()))
            .ReturnsAsync((AgentContext context) => context);

        this.directionOrchestrationServiceMock.InSequence(sequence)
            .Setup(service => service.ActAsync(It.IsAny<AgentContext>()))
            .ReturnsAsync((AgentContext context) =>
                context with { Result = "done", Status = AgentStatus.Responded });

        // when
        string actualResult =
            await this.agentCoordinationService.ProcessPromptAsync(randomPrompt);

        // then
        actualResult.Should().Be("done");
    }

    [Theory]
    [InlineData(AgentStatus.Responded)]
    [InlineData(AgentStatus.Refused)]
    [InlineData(AgentStatus.Failed)]
    [InlineData(AgentStatus.AwaitingInput)]
    public async Task ShouldBreakLoopOnProcessPromptIfStatusIsNotWorkingAsync(
AgentStatus terminalStatus)
    {
        // given
        string randomPrompt = CreateRandomString();
        SetupOrchestrationsPassThrough();

        this.directionOrchestrationServiceMock.Setup(service =>
            service.ActAsync(It.IsAny<AgentContext>()))
                .ReturnsAsync((AgentContext context) =>
                    context with { Result = "stopped", Status = terminalStatus });

        // when
        string actualResult =
            await this.agentCoordinationService.ProcessPromptAsync(randomPrompt);

        // then
        actualResult.Should().Be("stopped");

        this.dataOrchestrationServiceMock.Verify(service =>
            service.RecallAsync(It.IsAny<AgentContext>()),
                Times.Once);
    }

    [Fact]
    public async Task ShouldCapTurnsOnProcessPromptIfBrainNeverTerminatesAsync()
    {
        // given
        string randomPrompt = CreateRandomString();
        SetupOrchestrationsPassThrough();
        SetupDirectionNeverTerminates("again");

        // when
        string actualResult =
            await this.agentCoordinationService.ProcessPromptAsync(randomPrompt);

        // then
        actualResult.Should().Be("again");

        this.dataOrchestrationServiceMock.Verify(service =>
service.RecallAsync(It.IsAny<AgentContext>()),
Times.Exactly(7));

        this.decisionOrchestrationServiceMock.Verify(service =>
            service.ThinkAsync(It.IsAny<AgentContext>()),
                Times.Exactly(7));
    }

    [Fact]
    public async Task ShouldRecallEveryTurnOnProcessPromptAsync()
    {
        // given
        string randomPrompt = CreateRandomString();
        int turn = 0;

        this.dataOrchestrationServiceMock.Setup(service =>
            service.RecallAsync(It.IsAny<AgentContext>()))
                .ReturnsAsync((AgentContext context) => context);

        this.decisionOrchestrationServiceMock.Setup(service =>
            service.ThinkAsync(It.IsAny<AgentContext>()))
                .ReturnsAsync((AgentContext context) => context);

        this.directionOrchestrationServiceMock.Setup(service =>
    service.ActAsync(It.IsAny<AgentContext>()))
        .ReturnsAsync((AgentContext context) =>
        {
            turn++;

            return turn < 3
                ? context with { Result = "working", Status = AgentStatus.Working }
                : context with { Result = "done", Status = AgentStatus.Responded };
        });

        // when
        string actualResult =
            await this.agentCoordinationService.ProcessPromptAsync(randomPrompt);

        // then
        actualResult.Should().Be("done");

        this.dataOrchestrationServiceMock.Verify(service =>
            service.RecallAsync(It.IsAny<AgentContext>()),
                Times.Exactly(3));
    }

    [Fact]
    public async Task ShouldResetLogOnProcessPromptAsync()
    {
        // given
        string randomPrompt = CreateRandomString();
        SetupOrchestrationsPassThrough();
        SetupDirectionTerminates(CreateRandomString());

        // when
        await this.agentCoordinationService.ProcessPromptAsync(randomPrompt);

        // then
        this.logBrokerMock.Verify(broker =>
            broker.ResetAsync(),
                Times.Once);
    }

    [Fact]
    public async Task ShouldWriteTurnToLogOnProcessPromptAsync()
    {
        // given
        string randomPrompt = CreateRandomString();
        SetupOrchestrationsPassThrough();
        SetupDirectionTerminates(CreateRandomString());

        // when
        await this.agentCoordinationService.ProcessPromptAsync(randomPrompt);

        // then
        this.logBrokerMock.Verify(broker =>
            broker.WriteAsync(It.IsAny<string>()),
                Times.AtLeastOnce);
    }

    [Fact]
    public async Task ShouldNotLeakStateBetweenPromptsOnProcessPromptAsync()
    {
        // given
        string firstPrompt = CreateRandomString();
        string secondPrompt = CreateRandomString();
        List<AgentContext> recalledContexts = [];

        this.dataOrchestrationServiceMock.Setup(service =>
            service.RecallAsync(It.IsAny<AgentContext>()))
                .ReturnsAsync((AgentContext context) =>
                {
                    recalledContexts.Add(context);

                    return context with
                    {
                        Observations = [.. context.Observations, "some observation"]
                    };
                });

        this.decisionOrchestrationServiceMock.Setup(service =>
            service.ThinkAsync(It.IsAny<AgentContext>()))
                .ReturnsAsync((AgentContext context) => context);

        SetupDirectionTerminates("done");

        // when
        await this.agentCoordinationService.ProcessPromptAsync(firstPrompt);
        await this.agentCoordinationService.ProcessPromptAsync(secondPrompt);

        // then
        recalledContexts.Should().HaveCount(2);
        recalledContexts[0].Prompt.Should().Be(firstPrompt);
        recalledContexts[1].Prompt.Should().Be(secondPrompt);

        recalledContexts[1].Observations.Should().BeEmpty();
    }
}
