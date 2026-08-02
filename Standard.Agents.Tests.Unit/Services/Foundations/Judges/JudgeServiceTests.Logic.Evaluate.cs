// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Globalization;
using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Judges;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Judges;

public partial class JudgeServiceTests
{
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.3)]
    [InlineData(1.0)]
    public async Task ShouldEvaluateAsync(double score)
    {
        // given
        string randomCandidate = CreateRandomString();
        string verdict = score.ToString(CultureInfo.InvariantCulture);
        double expectedScore = score;

        this.verifierBrokerMock.Setup(broker =>
            broker.VerifyAsync(randomCandidate))
                .ReturnsAsync(verdict);

        // when
        Judgement actualJudgement =
            await this.judgeService.EvaluateAsync(randomCandidate);

        // then
        actualJudgement.Score.Should().Be(expectedScore);

        this.verifierBrokerMock.Verify(broker =>
            broker.VerifyAsync(randomCandidate),
                Times.Once);

        this.verifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(" 0.75 ", 0.75)]
    [InlineData("0.5\n", 0.5)]
    public async Task ShouldParseSurroundingWhitespaceOnEvaluateAsync(
        string verdict,
        double expectedScore)
    {
        // given
        string randomCandidate = CreateRandomString();

        this.verifierBrokerMock.Setup(broker =>
            broker.VerifyAsync(randomCandidate))
                .ReturnsAsync(verdict);

        // when
        Judgement actualJudgement =
            await this.judgeService.EvaluateAsync(randomCandidate);

        // then
        actualJudgement.Score.Should().Be(expectedScore);

        this.verifierBrokerMock.Verify(broker =>
            broker.VerifyAsync(randomCandidate),
                Times.Once);

        this.verifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldParseScoreAndReasonOnEvaluateAsync()
    {
        // given
        string randomCandidate = CreateRandomString();
        string verdict = "SCORE: 0.4\nREASON: it never states the year";

        this.verifierBrokerMock.Setup(broker =>
            broker.VerifyAsync(randomCandidate))
                .ReturnsAsync(verdict);

        // when
        Judgement actualJudgement =
            await this.judgeService.EvaluateAsync(randomCandidate);

        // then
        actualJudgement.Score.Should().Be(0.4);
        actualJudgement.Reason.Should().Be("it never states the year");

        this.verifierBrokerMock.Verify(broker =>
            broker.VerifyAsync(randomCandidate),
                Times.Once);

        this.verifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
