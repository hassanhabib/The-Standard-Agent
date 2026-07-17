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

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Direction;

public partial class DirectionOrchestrationServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnActIfContextIsNullAndLogItAsync()
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
        ValueTask<AgentContext> actTask =
            this.directionOrchestrationService.ActAsync(nullContext!);

        AgentOrchestrationValidationException actualException =
            await Assert.ThrowsAsync<AgentOrchestrationValidationException>(
                actTask.AsTask);

        // then
        actualException.Should()
            .BeEquivalentTo(expectedAgentOrchestrationValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedAgentOrchestrationValidationException))),
                    Times.Once);

        this.internalToolServiceMock.Verify(service =>
            service.HandlesAsync(It.IsAny<string>()),
                Times.Never);

        this.internalToolServiceMock.VerifyNoOtherCalls();
        this.externalToolServiceMock.VerifyNoOtherCalls();
        this.returnServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(DependencyValidationExceptions))]
    public async Task ShouldThrowDependencyValidationExceptionOnActIfDependencyValidationErrorOccursAndLogItAsync(
Xeption foundationException)
    {
        // given
        AgentContext inputContext = CreateContextWithDirection("calculator", "1+1");

        var expectedException =
            new AgentOrchestrationDependencyValidationException(
                message: "Agent orchestration dependency validation error occurred, fix the error and try again.",
                innerException: foundationException.InnerException as Xeption);

        this.internalToolServiceMock.Setup(service =>
            service.HandlesAsync(It.IsAny<string>()))
                .ThrowsAsync(foundationException);

        // when
        ValueTask<AgentContext> actTask =
            this.directionOrchestrationService.ActAsync(inputContext);

        AgentOrchestrationDependencyValidationException actualException =
            await Assert.ThrowsAsync<AgentOrchestrationDependencyValidationException>(
                actTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(DependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnActIfDependencyErrorOccursAndLogItAsync(
        Xeption foundationException)
    {
        // given
        AgentContext inputContext = CreateContextWithDirection("calculator", "1+1");

        var expectedException =
            new AgentOrchestrationDependencyException(
                message: "Agent orchestration dependency error occurred, contact support.",
                innerException: foundationException.InnerException as Xeption);

        this.internalToolServiceMock.Setup(service =>
            service.HandlesAsync(It.IsAny<string>()))
                .ThrowsAsync(foundationException);

        // when
        ValueTask<AgentContext> actTask =
            this.directionOrchestrationService.ActAsync(inputContext);

        AgentOrchestrationDependencyException actualException =
            await Assert.ThrowsAsync<AgentOrchestrationDependencyException>(
                actTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnActIfServiceErrorOccursAndLogItAsync()
    {
        // given
        AgentContext inputContext = CreateContextWithDirection("calculator", "1+1");
        var serviceException = new Exception();

        var failedAgentOrchestrationServiceException =
            new FailedAgentOrchestrationServiceException(
                message: "Failed agent orchestration service error occurred, contact support.",
                innerException: serviceException);

        var expectedException =
            new AgentOrchestrationServiceException(
                message: "Agent orchestration service error occurred, contact support.",
                innerException: failedAgentOrchestrationServiceException);

        this.internalToolServiceMock.Setup(service =>
            service.HandlesAsync(It.IsAny<string>()))
                .ThrowsAsync(serviceException);

        // when
        ValueTask<AgentContext> actTask =
            this.directionOrchestrationService.ActAsync(inputContext);

        AgentOrchestrationServiceException actualException =
            await Assert.ThrowsAsync<AgentOrchestrationServiceException>(
                actTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
    }
