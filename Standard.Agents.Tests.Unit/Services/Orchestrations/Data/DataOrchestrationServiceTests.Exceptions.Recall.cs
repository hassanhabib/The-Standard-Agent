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

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Data;

public partial class DataOrchestrationServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnRecallIfContextIsNullAndLogItAsync()
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
        ValueTask<AgentContext> recallTask =
            this.dataOrchestrationService.RecallAsync(nullContext!);

        AgentOrchestrationValidationException actualException =
            await Assert.ThrowsAsync<AgentOrchestrationValidationException>(
                recallTask.AsTask);

        // then
        actualException.Should()
            .BeEquivalentTo(expectedAgentOrchestrationValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedAgentOrchestrationValidationException))),
                    Times.Once);

        this.skillServiceMock.Verify(service =>
            service.RetrieveSkillsAsync(),
                Times.Never);

        this.skillServiceMock.VerifyNoOtherCalls();
        this.memoryServiceMock.VerifyNoOtherCalls();
        this.knowledgeServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    // Unwrap the foundation's categorical exception, preserve its LOCAL exception as
    // the inner, rewrap in this layer's category. The local exception must survive so
    // detail is not lost climbing the tiers.
    [Theory]
    [MemberData(nameof(DependencyValidationExceptions))]
    public async Task ShouldThrowDependencyValidationExceptionOnRecallIfDependencyValidationErrorOccursAndLogItAsync(
        Xeption foundationException)
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();

        var expectedException =
            new AgentOrchestrationDependencyValidationException(
                message: "Agent orchestration dependency validation error occurred, fix the error and try again.",
                innerException: foundationException.InnerException as Xeption);

        this.skillServiceMock.Setup(service =>
            service.RetrieveSkillsAsync())
                .ThrowsAsync(foundationException);

        // when
        ValueTask<AgentContext> recallTask =
            this.dataOrchestrationService.RecallAsync(inputContext);

        AgentOrchestrationDependencyValidationException actualException =
            await Assert.ThrowsAsync<AgentOrchestrationDependencyValidationException>(
                recallTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(DependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnRecallIfDependencyErrorOccursAndLogItAsync(
        Xeption foundationException)
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();

        var expectedException =
            new AgentOrchestrationDependencyException(
                message: "Agent orchestration dependency error occurred, contact support.",
                innerException: foundationException.InnerException as Xeption);

        this.skillServiceMock.Setup(service =>
            service.RetrieveSkillsAsync())
                .ThrowsAsync(foundationException);

        // when
        ValueTask<AgentContext> recallTask =
            this.dataOrchestrationService.RecallAsync(inputContext);

        AgentOrchestrationDependencyException actualException =
            await Assert.ThrowsAsync<AgentOrchestrationDependencyException>(
                recallTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnRecallIfServiceErrorOccursAndLogItAsync()
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

        this.skillServiceMock.Setup(service =>
            service.RetrieveSkillsAsync())
                .ThrowsAsync(serviceException);

        // when
        ValueTask<AgentContext> recallTask =
            this.dataOrchestrationService.RecallAsync(inputContext);

        AgentOrchestrationServiceException actualException =
            await Assert.ThrowsAsync<AgentOrchestrationServiceException>(
                recallTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
