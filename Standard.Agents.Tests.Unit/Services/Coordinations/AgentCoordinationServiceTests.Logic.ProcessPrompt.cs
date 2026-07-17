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
    // Vector 01 in miniature: one turn, terminal, answer returned.
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

    // The prompt reaches Recall as the context's Prompt, and Status starts Working —
    // SPEC.md 3 requires Working to be the initial value, and 5 starts the loop from
    // a fresh context carrying only the prompt.
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

    // Enforced order. All three take and return AgentContext, so the sequence is NOT
    // encoded in their signatures — nothing would stop Act running before Think and
    // the types would still line up. The Standard requires an explicit order test
    // exactly when order is not naturally encoded.
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

    // The loop stops on any non-Working status. SPEC.md 3: "continues while
    // status == Working and stops on any other value" — so a new terminal state can
    // be added to the enum without touching the loop.
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

    // Vector 06. A Brain that never terminates MUST be capped — SPEC.md 5 makes the
    // cap a MUST. Without it this test would hang forever rather than fail, which is
    // why the loop is the one place a bound is not optional.
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

        // Capped, not infinite. The exact bound is ours (SPEC.md 5: finite, SHOULD be
        // small); what matters is that it is finite and the same on every path.
        this.dataOrchestrationServiceMock.Verify(service =>
            service.RecallAsync(It.IsAny<AgentContext>()),
                Times.Exactly(7));

        this.decisionOrchestrationServiceMock.Verify(service =>
            service.ThinkAsync(It.IsAny<AgentContext>()),
                Times.Exactly(7));
    }

    // Recall runs every turn, not once. SPEC.md 5 puts it inside the loop so skills
    // refresh per turn; hoisting it out as an optimisation would break that.
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

        // Two working turns, then terminate.
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

    // The log is reset once per prompt, at the top — SPEC.md 5. Each prompt's trace
    // is its own narrative, not an append to the last one's.
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

    // SPEC.md 4.4: Coordination MUST hold no nature logic beyond sequencing and
    // observing. This is the observing half — one line per turn.
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

    // ⭐ Invariant 7.4 — the agent instance is ephemeral and MUST be stateless across
    // prompts. Two sequential prompts on the SAME instance must not leak: if the
    // second saw the first's observations, the agent would answer a question nobody
    // asked and the leak would grow with every prompt.
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

        // The second prompt starts clean — no observations carried from the first.
        recalledContexts[1].Observations.Should().BeEmpty();
    }
}
