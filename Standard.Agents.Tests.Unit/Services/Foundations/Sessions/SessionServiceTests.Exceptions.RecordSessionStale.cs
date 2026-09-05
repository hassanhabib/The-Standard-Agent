// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Data;
using FluentAssertions;
using Moq;
using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Foundations.Sessions.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Sessions;

public partial class SessionServiceTests
{
    // Found in the 2026-09-04 principal review (F-06): a store that refuses a write based on a
    // stale read is the caller's problem to retry, not an outage. The native concurrency
    // signal is localized into a session failure and categorized as dependency validation, so
    // the tier above can tell "try again" from "the disk is gone".
    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnRecordSessionIfSessionIsStaleAndLogItAsync()
    {
        // given
        AgentSession randomSession = CreateRandomSession();
        var dbConcurrencyException = new DBConcurrencyException("the session moved on");

        var staleSessionException =
            new StaleSessionException(
                message: "The session was written by another prompt since it was read; read it again and retry.",
                innerException: dbConcurrencyException);

        var expectedSessionDependencyValidationException =
            new SessionDependencyValidationException(
                message: "Session dependency validation error occurred, fix the error and try again.",
                innerException: staleSessionException);

        this.sessionBrokerMock.Setup(broker =>
            broker.UpsertSessionAsync(randomSession))
                .ThrowsAsync(dbConcurrencyException);

        // when
        ValueTask recordTask =
            this.sessionService.RecordSessionAsync(randomSession);

        SessionDependencyValidationException actualSessionDependencyValidationException =
            await Assert.ThrowsAsync<SessionDependencyValidationException>(recordTask.AsTask);

        // then
        actualSessionDependencyValidationException.Should()
            .BeEquivalentTo(expectedSessionDependencyValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedSessionDependencyValidationException))),
                    Times.Once);

        this.sessionBrokerMock.Verify(broker =>
            broker.UpsertSessionAsync(randomSession),
                Times.Once);

        this.sessionBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
