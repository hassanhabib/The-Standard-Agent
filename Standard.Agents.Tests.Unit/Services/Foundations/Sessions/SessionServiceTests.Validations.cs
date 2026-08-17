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

public partial class SessionServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task
        ShouldThrowValidationExceptionOnRetrieveSessionIfIdIsInvalidAndLogItAsync(
            string? invalidSessionId)
    {
        // given
        var invalidSessionException =
            new InvalidSessionException(
                message: "Invalid session. Please correct the error and try again.");

        var expectedSessionValidationException =
            new SessionValidationException(
                message: "Session validation error occurred, fix the error and try again.",
                innerException: invalidSessionException);

        // when
        ValueTask<AgentSession?> retrieveTask =
            this.sessionService.RetrieveSessionAsync(invalidSessionId!);

        SessionValidationException actualSessionValidationException =
            await Assert.ThrowsAsync<SessionValidationException>(retrieveTask.AsTask);

        // then
        actualSessionValidationException.Should()
            .BeEquivalentTo(expectedSessionValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedSessionValidationException))),
                    Times.Once);

        this.sessionBrokerMock.Verify(broker =>
            broker.SelectSessionAsync(It.IsAny<string>()),
                Times.Never);

        this.sessionBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    // A session written without an id cannot be read back. Nothing throws, the next prompt finds
    // nothing, and the agent forgets — with no error anywhere to explain it.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task
        ShouldThrowValidationExceptionOnRecordSessionIfIdIsInvalidAndLogItAsync(
            string? invalidSessionId)
    {
        // given
        var invalidSession = new AgentSession { Id = invalidSessionId! };

        var invalidSessionException =
            new InvalidSessionException(
                message: "Invalid session. Please correct the error and try again.");

        var expectedSessionValidationException =
            new SessionValidationException(
                message: "Session validation error occurred, fix the error and try again.",
                innerException: invalidSessionException);

        // when
        ValueTask recordTask = this.sessionService.RecordSessionAsync(invalidSession);

        SessionValidationException actualSessionValidationException =
            await Assert.ThrowsAsync<SessionValidationException>(recordTask.AsTask);

        // then
        actualSessionValidationException.Should()
            .BeEquivalentTo(expectedSessionValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedSessionValidationException))),
                    Times.Once);

        this.sessionBrokerMock.Verify(broker =>
            broker.UpsertSessionAsync(It.IsAny<AgentSession>()),
                Times.Never);

        this.sessionBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
