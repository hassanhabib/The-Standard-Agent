// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Usages;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Usages;

public partial class UsageServiceTests
{
    [Fact]
    public async Task ShouldMeasurePromptAndCompletionSeparatelyOnMeasureAsync()
    {
        // given
        string randomPrompt = CreateRandomString();
        string randomCompletion = CreateRandomString();
        int randomPromptTokens = CreateRandomTokenCount();
        int randomCompletionTokens = CreateRandomTokenCount();

        this.usageBrokerMock.Setup(broker =>
            broker.CountTokensAsync(randomPrompt))
                .ReturnsAsync(randomPromptTokens);

        this.usageBrokerMock.Setup(broker =>
            broker.CountTokensAsync(randomCompletion))
                .ReturnsAsync(randomCompletionTokens);

        // when
        AgentUsage actualUsage =
            await this.usageService.MeasureAsync(randomPrompt, randomCompletion);

        // then
        actualUsage.PromptTokens.Should().Be(randomPromptTokens);
        actualUsage.CompletionTokens.Should().Be(randomCompletionTokens);
        actualUsage.TotalTokens.Should().Be(randomPromptTokens + randomCompletionTokens);

        this.usageBrokerMock.Verify(broker =>
            broker.CountTokensAsync(randomPrompt),
                Times.Once);

        this.usageBrokerMock.Verify(broker =>
            broker.CountTokensAsync(randomCompletion),
                Times.Once);

        this.usageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    // A counted number and a reported one are both usable for a budget, but only one can be
    // reconciled against an invoice. Anything this service produces was counted here, so it says
    // so — a caller that cannot tell them apart will eventually present an estimate as a bill.
    [Fact]
    public async Task ShouldMarkAMeasuredCallAsEstimatedOnMeasureAsync()
    {
        // given
        string randomPrompt = CreateRandomString();
        string randomCompletion = CreateRandomString();

        this.usageBrokerMock.Setup(broker =>
            broker.CountTokensAsync(It.IsAny<string>()))
                .ReturnsAsync(CreateRandomTokenCount());

        // when
        AgentUsage actualUsage =
            await this.usageService.MeasureAsync(randomPrompt, randomCompletion);

        // then
        actualUsage.IsEstimated.Should().BeTrue();
    }

    // An empty completion is an ordinary measurement, not a missing one: a call that produced no
    // reply consumed no completion tokens, and it still consumed its prompt.
    [Fact]
    public async Task ShouldMeasureEmptyCompletionWithoutFailingOnMeasureAsync()
    {
        // given
        string randomPrompt = CreateRandomString();
        int randomPromptTokens = CreateRandomTokenCount();

        this.usageBrokerMock.Setup(broker =>
            broker.CountTokensAsync(randomPrompt))
                .ReturnsAsync(randomPromptTokens);

        this.usageBrokerMock.Setup(broker =>
            broker.CountTokensAsync(string.Empty))
                .ReturnsAsync(0);

        // when
        AgentUsage actualUsage =
            await this.usageService.MeasureAsync(randomPrompt, string.Empty);

        // then
        actualUsage.PromptTokens.Should().Be(randomPromptTokens);
        actualUsage.CompletionTokens.Should().Be(0);
    }
}
