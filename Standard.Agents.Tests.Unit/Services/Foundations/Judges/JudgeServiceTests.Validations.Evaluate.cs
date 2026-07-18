// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Globalization;
using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Judges.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Judges;

public partial class JudgeServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnEvaluateIfCandidateIsInvalidAndLogItAsync(
        string? invalidCandidate)
    {
        // given
        var invalidJudgeException =
            new InvalidJudgeException(
                message: "Invalid judge input. Please correct the error and try again.");

        var expectedJudgeValidationException =
            new JudgeValidationException(
                message: "Judge validation error occurred, fix the error and try again.",
                innerException: invalidJudgeException);

        // when
        ValueTask<double> evaluateTask =
            this.judgeService.EvaluateAsync(invalidCandidate!);

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
            broker.VerifyAsync(It.IsAny<string>()),
                Times.Never);

        this.verifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("allow")]
    [InlineData("ACTION: calculator: 47 * 89")]
    [InlineData("I would rate this a 9 out of 10.")]
    public async Task ShouldThrowValidationExceptionOnEvaluateIfScoreIsNotNumericAndLogItAsync(
        string? nonNumericVerdict)
    {
        // given
        string randomCandidate = CreateRandomString();

        var invalidJudgeScoreException =
            new InvalidJudgeScoreException(
                message: "Invalid judge score. Score must be a number between 0.0 and 1.0.");

        var expectedJudgeValidationException =
            new JudgeValidationException(
                message: "Judge validation error occurred, fix the error and try again.",
                innerException: invalidJudgeScoreException);

        this.verifierBrokerMock.Setup(broker =>
            broker.VerifyAsync(randomCandidate))
                .ReturnsAsync(nonNumericVerdict!);

        // when
        ValueTask<double> evaluateTask =
            this.judgeService.EvaluateAsync(randomCandidate);

        JudgeValidationException actualJudgeValidationException =
            await Assert.ThrowsAsync<JudgeValidationException>(
                evaluateTask.AsTask);

        // then
        actualJudgeValidationException.Should()
            .BeEquivalentTo(expectedJudgeValidationException);

        this.verifierBrokerMock.Verify(broker =>
            broker.VerifyAsync(randomCandidate),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedJudgeValidationException))),
                    Times.Once);

        this.verifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(87)]
    [InlineData(double.NaN)]
    public async Task ShouldThrowValidationExceptionOnEvaluateIfScoreIsOutOfRangeAndLogItAsync(
        double outOfRangeScore)
    {
        // given
        string randomCandidate = CreateRandomString();
        string verdict = outOfRangeScore.ToString(CultureInfo.InvariantCulture);

        var invalidJudgeScoreException =
            new InvalidJudgeScoreException(
                message: "Invalid judge score. Score must be between 0.0 and 1.0.");

        var expectedJudgeValidationException =
            new JudgeValidationException(
                message: "Judge validation error occurred, fix the error and try again.",
                innerException: invalidJudgeScoreException);

        this.verifierBrokerMock.Setup(broker =>
            broker.VerifyAsync(randomCandidate))
                .ReturnsAsync(verdict);

        // when
        ValueTask<double> evaluateTask =
            this.judgeService.EvaluateAsync(randomCandidate);

        JudgeValidationException actualJudgeValidationException =
            await Assert.ThrowsAsync<JudgeValidationException>(
                evaluateTask.AsTask);

        // then
        actualJudgeValidationException.Should()
            .BeEquivalentTo(expectedJudgeValidationException);

        this.verifierBrokerMock.Verify(broker =>
            broker.VerifyAsync(randomCandidate),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedJudgeValidationException))),
                    Times.Once);

        this.verifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
