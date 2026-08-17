// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Foundations.Sessions.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Sessions;

// The whole point of the foundation. A store failure has to arrive as a SESSION failure — an
// IOException surfacing unmapped is what let a full disk get blamed on the loop.
public partial class SessionServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyExceptionOnRetrieveSessionIfIOErrorOccursAsync()
    {
        // given
        string randomSessionId = CreateRandomString();
        var ioException = new IOException();

        var failedSessionDependencyException =
            new FailedSessionDependencyException(
                message: "Failed session dependency error occurred, contact support.",
                innerException: ioException);

        var expectedSessionDependencyException =
            new SessionDependencyException(
                message: "Session dependency error occurred, contact support.",
                innerException: failedSessionDependencyException);

        this.sessionBrokerMock.Setup(broker =>
            broker.SelectSessionAsync(It.IsAny<string>()))
                .ThrowsAsync(ioException);

        // when
        ValueTask<AgentSession?> retrieveTask =
            this.sessionService.RetrieveSessionAsync(randomSessionId);

        SessionDependencyException actualSessionDependencyException =
            await Assert.ThrowsAsync<SessionDependencyException>(retrieveTask.AsTask);

        // then
        actualSessionDependencyException.Should()
            .BeEquivalentTo(expectedSessionDependencyException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedSessionDependencyException))),
                    Times.Once);

        this.sessionBrokerMock.Verify(broker =>
            broker.SelectSessionAsync(randomSessionId),
                Times.Once);

        this.sessionBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task
        ShouldThrowCriticalDependencyExceptionOnRecordSessionIfAccessDeniedAsync()
    {
        // given
        AgentSession randomSession = CreateRandomSession();
        var unauthorizedAccessException = new UnauthorizedAccessException();

        var failedSessionDependencyException =
            new FailedSessionDependencyException(
                message: "Failed session dependency error occurred, contact support.",
                innerException: unauthorizedAccessException);

        var expectedSessionDependencyException =
            new SessionDependencyException(
                message: "Session dependency error occurred, contact support.",
                innerException: failedSessionDependencyException);

        this.sessionBrokerMock.Setup(broker =>
            broker.UpsertSessionAsync(It.IsAny<AgentSession>()))
                .ThrowsAsync(unauthorizedAccessException);

        // when
        ValueTask recordTask = this.sessionService.RecordSessionAsync(randomSession);

        SessionDependencyException actualSessionDependencyException =
            await Assert.ThrowsAsync<SessionDependencyException>(recordTask.AsTask);

        // then
        actualSessionDependencyException.Should()
            .BeEquivalentTo(expectedSessionDependencyException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(
                expectedSessionDependencyException))),
                    Times.Once);

        this.sessionBrokerMock.Verify(broker =>
            broker.UpsertSessionAsync(randomSession),
                Times.Once);

        this.sessionBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnRetrieveSessionIfServiceErrorOccursAsync()
    {
        // given
        string randomSessionId = CreateRandomString();
        var serviceException = new Exception();

        var failedSessionServiceException =
            new FailedSessionServiceException(
                message: "Failed session service error occurred, contact support.",
                innerException: serviceException);

        var expectedSessionServiceException =
            new SessionServiceException(
                message: "Session service error occurred, contact support.",
                innerException: failedSessionServiceException);

        this.sessionBrokerMock.Setup(broker =>
            broker.SelectSessionAsync(It.IsAny<string>()))
                .ThrowsAsync(serviceException);

        // when
        ValueTask<AgentSession?> retrieveTask =
            this.sessionService.RetrieveSessionAsync(randomSessionId);

        SessionServiceException actualSessionServiceException =
            await Assert.ThrowsAsync<SessionServiceException>(retrieveTask.AsTask);

        // then
        actualSessionServiceException.Should()
            .BeEquivalentTo(expectedSessionServiceException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedSessionServiceException))),
                    Times.Once);

        this.sessionBrokerMock.Verify(broker =>
            broker.SelectSessionAsync(randomSessionId),
                Times.Once);

        this.sessionBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
