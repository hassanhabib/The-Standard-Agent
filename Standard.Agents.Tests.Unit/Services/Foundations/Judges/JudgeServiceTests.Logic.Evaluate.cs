// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
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
        string randomJudgePrompt = CreateRandomString();
        string randomCandidate = CreateRandomString();
        double expectedScore = score;

        this.verifierBrokerMock.Setup(broker =>
            broker.VerifyAsync(randomJudgePrompt, randomCandidate))
                .ReturnsAsync(score);

        // when
        double actualScore =
            await this.judgeService.EvaluateAsync(randomJudgePrompt, randomCandidate);

        // then
        actualScore.Should().Be(expectedScore);

        this.verifierBrokerMock.Verify(broker =>
            broker.VerifyAsync(randomJudgePrompt, randomCandidate),
                Times.Once);

        this.verifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldPassJudgePromptThroughUnalteredOnEvaluateAsync()
    {
        // given
        string judgePromptWithFormatting = "  Score 0..1 for groundedness.\n\n- cite sources  ";
        string randomCandidate = CreateRandomString();
        double expectedScore = 0.75;

        this.verifierBrokerMock.Setup(broker =>
            broker.VerifyAsync(judgePromptWithFormatting, randomCandidate))
                .ReturnsAsync(expectedScore);

        // when
        double actualScore = await this.judgeService.EvaluateAsync(
            judgePromptWithFormatting,
            randomCandidate);

        // then
        actualScore.Should().Be(expectedScore);

        this.verifierBrokerMock.Verify(broker =>
            broker.VerifyAsync(judgePromptWithFormatting, randomCandidate),
                Times.Once);

        this.verifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
