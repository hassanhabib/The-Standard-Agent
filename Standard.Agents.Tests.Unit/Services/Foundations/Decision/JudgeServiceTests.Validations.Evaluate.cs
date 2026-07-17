// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Judges.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Decision;

public partial class JudgeServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnEvaluateIfJudgePromptIsInvalidAndLogItAsync(
        string? invalidJudgePrompt)
    {
        // given
        string randomCandidate = CreateRandomString();

        var invalidJudgeException =
            new InvalidJudgeException(
                message: "Invalid judge input. Please correct the error and try again.");

        var expectedJudgeValidationException =
            new JudgeValidationException(
                message: "Judge validation error occurred, fix the error and try again.",
                innerException: invalidJudgeException);

        // when
        ValueTask<double> evaluateTask =
            this.judgeService.EvaluateAsync(invalidJudgePrompt!, randomCandidate);

        JudgeValidationException actualJudgeValidationException =
            await Assert.ThrowsAsync<JudgeValidationException>(
                evaluateTask.AsTask);

        // then
        actualJudgeValidationException.Should()
            .BeEquivalentTo(expectedJudgeValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedJudgeValidationException))),
                    Times.Once);

        this.verifierBrokerMock.Verify(broker =>
            broker.VerifyAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

        this.verifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnEvaluateIfCandidateIsInvalidAndLogItAsync(
        string? invalidCandidate)
    {
        // given
        string randomJudgePrompt = CreateRandomString();

        var invalidJudgeException =
            new InvalidJudgeException(
                message: "Invalid judge input. Please correct the error and try again.");

        var expectedJudgeValidationException =
            new JudgeValidationException(
                message: "Judge validation error occurred, fix the error and try again.",
                innerException: invalidJudgeException);

        // when
        ValueTask<double> evaluateTask =
            this.judgeService.EvaluateAsync(randomJudgePrompt, invalidCandidate!);

        JudgeValidationException actualJudgeValidationException =
            await Assert.ThrowsAsync<JudgeValidationException>(
                evaluateTask.AsTask);

        // then
        actualJudgeValidationException.Should()
            .BeEquivalentTo(expectedJudgeValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedJudgeValidationException))),
                    Times.Once);

        this.verifierBrokerMock.Verify(broker =>
            broker.VerifyAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

        this.verifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    // ⭐ An OUTGOING validation — the only one in the codebase. The Standard:
    // "Validate outgoing data when the current routine uses it." 0.0..1.0 is
    // normative in SPEC.md 4.1, and the broker cannot enforce it (no flow control).
    //
    // This is not pedantry. ThinkAsync gates on `score < 0.3` (#33). A verifier
    // returning 87 — a model answering out of 100 instead of 0..1 — would sail past
    // that gate and every draft would pass review forever, silently.
    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(87)]
    [InlineData(double.NaN)]
    public async Task ShouldThrowValidationExceptionOnEvaluateIfScoreIsOutOfRangeAndLogItAsync(
        double outOfRangeScore)
    {
        // given
        string randomJudgePrompt = CreateRandomString();
        string randomCandidate = CreateRandomString();

        var invalidJudgeScoreException =
            new InvalidJudgeScoreException(
                message: "Invalid judge score. Score must be between 0.0 and 1.0.");

        var expectedJudgeValidationException =
            new JudgeValidationException(
                message: "Judge validation error occurred, fix the error and try again.",
                innerException: invalidJudgeScoreException);

        this.verifierBrokerMock.Setup(broker =>
            broker.VerifyAsync(randomJudgePrompt, randomCandidate))
                .ReturnsAsync(outOfRangeScore);

        // when
        ValueTask<double> evaluateTask =
            this.judgeService.EvaluateAsync(randomJudgePrompt, randomCandidate);

        JudgeValidationException actualJudgeValidationException =
            await Assert.ThrowsAsync<JudgeValidationException>(
                evaluateTask.AsTask);

        // then
        actualJudgeValidationException.Should()
            .BeEquivalentTo(expectedJudgeValidationException);

        this.verifierBrokerMock.Verify(broker =>
            broker.VerifyAsync(randomJudgePrompt, randomCandidate),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedJudgeValidationException))),
                    Times.Once);

        this.verifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
