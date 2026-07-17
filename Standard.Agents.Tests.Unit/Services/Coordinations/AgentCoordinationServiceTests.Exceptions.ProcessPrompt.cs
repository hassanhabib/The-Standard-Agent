// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Coordinations.Agents.Exceptions;
using Standard.Agents.Models.Orchestrations.Agents;
using Xeptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Coordinations;

public partial class AgentCoordinationServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnProcessPromptIfPromptIsInvalidAndLogItAsync(
        string? invalidPrompt)
    {
        // given
        var invalidAgentException =
            new InvalidAgentException(
                message: "Invalid prompt. Please correct the error and try again.");

        var expectedException =
            new AgentCoordinationValidationException(
                message: "Agent coordination validation error occurred, fix the error and try again.",
                innerException: invalidAgentException);

        // when
        ValueTask<string> processTask =
            this.agentCoordinationService.ProcessPromptAsync(invalidPrompt!);

        AgentCoordinationValidationException actualException =
            await Assert.ThrowsAsync<AgentCoordinationValidationException>(
                processTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

        // Nothing ran — not even the log reset. An empty prompt is rejected before
        // the loop touches anything.
        this.logBrokerMock.VerifyNoOtherCalls();
        this.dataOrchestrationServiceMock.VerifyNoOtherCalls();
    }

    // An unrecoverable failure reaches the caller as one typed exception. This is the
    // other half of the #34 decision: nothing sets AgentStatus.Failed, so THIS is
    // where a failed turn surfaces.
    [Theory]
    [MemberData(nameof(DependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnProcessPromptIfDependencyErrorOccursAndLogItAsync(
        Xeption orchestrationException)
    {
        // given
        string randomPrompt = CreateRandomString();

        var expectedException =
            new AgentCoordinationDependencyException(
                message: "Agent coordination dependency error occurred, contact support.",
                innerException: orchestrationException.InnerException as Xeption);

        this.dataOrchestrationServiceMock.Setup(service =>
            service.RecallAsync(It.IsAny<AgentContext>()))
                .ThrowsAsync(orchestrationException);

        // when
        ValueTask<string> processTask =
            this.agentCoordinationService.ProcessPromptAsync(randomPrompt);

        AgentCoordinationDependencyException actualException =
            await Assert.ThrowsAsync<AgentCoordinationDependencyException>(
                processTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnProcessPromptIfServiceErrorOccursAndLogItAsync()
    {
        // given
        string randomPrompt = CreateRandomString();
        var serviceException = new Exception();

        var failedAgentCoordinationServiceException =
            new FailedAgentCoordinationServiceException(
                message: "Failed agent coordination service error occurred, contact support.",
                innerException: serviceException);

        var expectedException =
            new AgentCoordinationServiceException(
                message: "Agent coordination service error occurred, contact support.",
                innerException: failedAgentCoordinationServiceException);

        this.dataOrchestrationServiceMock.Setup(service =>
            service.RecallAsync(It.IsAny<AgentContext>()))
                .ThrowsAsync(serviceException);

        // when
        ValueTask<string> processTask =
            this.agentCoordinationService.ProcessPromptAsync(randomPrompt);

        AgentCoordinationServiceException actualException =
            await Assert.ThrowsAsync<AgentCoordinationServiceException>(
                processTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);
    }
}
