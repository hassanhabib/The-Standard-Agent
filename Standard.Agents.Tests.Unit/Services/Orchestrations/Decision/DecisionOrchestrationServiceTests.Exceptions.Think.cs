// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Agents.Exceptions;
using Xeptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Decision;

public partial class DecisionOrchestrationServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnThinkIfContextIsNullAndLogItAsync()
    {
        // given
        AgentContext? nullContext = null;

        var nullAgentContextException =
            new NullAgentContextException(
                message: "Agent context is null.");

        var expectedAgentOrchestrationValidationException =
            new AgentOrchestrationValidationException(
                message: "Agent orchestration validation error occurred, fix the error and try again.",
                innerException: nullAgentContextException);

        // when
        ValueTask<AgentContext> thinkTask =
            this.decisionOrchestrationService.ThinkAsync(nullContext!);

        AgentOrchestrationValidationException actualException =
            await Assert.ThrowsAsync<AgentOrchestrationValidationException>(
                thinkTask.AsTask);

        // then
        actualException.Should()
            .BeEquivalentTo(expectedAgentOrchestrationValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedAgentOrchestrationValidationException))),
                    Times.Once);

        this.gateServiceMock.Verify(service =>
            service.ScreenAsync(It.IsAny<string>()),
                Times.Never);

        this.gateServiceMock.VerifyNoOtherCalls();
        this.brainServiceMock.VerifyNoOtherCalls();
        this.judgeServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(DependencyValidationExceptions))]
    public async Task ShouldThrowDependencyValidationExceptionOnThinkIfDependencyValidationErrorOccursAndLogItAsync(
Xeption foundationException)
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();

        var expectedException =
            new AgentOrchestrationDependencyValidationException(
                message: "Agent orchestration dependency validation error occurred, fix the error and try again.",
                innerException: foundationException.InnerException as Xeption);

        this.gateServiceMock.Setup(service =>
            service.ScreenAsync(It.IsAny<string>()))
                .ThrowsAsync(foundationException);

        // when
        ValueTask<AgentContext> thinkTask =
            this.decisionOrchestrationService.ThinkAsync(inputContext);

        AgentOrchestrationDependencyValidationException actualException =
            await Assert.ThrowsAsync<AgentOrchestrationDependencyValidationException>(
                thinkTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(DependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnThinkIfDependencyErrorOccursAndLogItAsync(
        Xeption foundationException)
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();

        var expectedException =
            new AgentOrchestrationDependencyException(
                message: "Agent orchestration dependency error occurred, contact support.",
                innerException: foundationException.InnerException as Xeption);

        this.gateServiceMock.Setup(service =>
            service.ScreenAsync(It.IsAny<string>()))
                .ThrowsAsync(foundationException);

        // when
        ValueTask<AgentContext> thinkTask =
            this.decisionOrchestrationService.ThinkAsync(inputContext);

        AgentOrchestrationDependencyException actualException =
            await Assert.ThrowsAsync<AgentOrchestrationDependencyException>(
                thinkTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnThinkIfServiceErrorOccursAndLogItAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        var serviceException = new Exception();

        var failedAgentOrchestrationServiceException =
            new FailedAgentOrchestrationServiceException(
                message: "Failed agent orchestration service error occurred, contact support.",
                innerException: serviceException);

        var expectedException =
            new AgentOrchestrationServiceException(
                message: "Agent orchestration service error occurred, contact support.",
                innerException: failedAgentOrchestrationServiceException);

        this.gateServiceMock.Setup(service =>
            service.ScreenAsync(It.IsAny<string>()))
                .ThrowsAsync(serviceException);

        // when
        ValueTask<AgentContext> thinkTask =
            this.decisionOrchestrationService.ThinkAsync(inputContext);

        AgentOrchestrationServiceException actualException =
            await Assert.ThrowsAsync<AgentOrchestrationServiceException>(
                thinkTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
    }
