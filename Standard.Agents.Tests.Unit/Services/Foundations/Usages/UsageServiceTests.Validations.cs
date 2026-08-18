// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Usages.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Usages;

public partial class UsageServiceTests
{
    [Fact]
    public async Task ShouldThrowValidationExceptionOnMeasureIfPromptIsNullAsync()
    {
        // given
        var invalidUsageException =
            new InvalidUsageException(
                message: "Invalid usage, prompt is required. "
                    + "Please correct the error and try again.");

        var expectedUsageValidationException =
            new UsageValidationException(
                message: "Usage validation error occurred, fix the error and try again.",
                innerException: invalidUsageException);

        // when
        ValueTask<Models.Foundations.Usages.AgentUsage> measureTask =
            this.usageService.MeasureAsync(prompt: null!, completion: CreateRandomString());

        UsageValidationException actualException =
            await Assert.ThrowsAsync<UsageValidationException>(measureTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedUsageValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedUsageValidationException))),
                Times.Once);

        this.usageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowValidationExceptionOnMeasureIfCompletionIsNullAsync()
    {
        // given
        var invalidUsageException =
            new InvalidUsageException(
                message: "Invalid usage, completion is required. "
                    + "Please correct the error and try again.");

        var expectedUsageValidationException =
            new UsageValidationException(
                message: "Usage validation error occurred, fix the error and try again.",
                innerException: invalidUsageException);

        // when
        ValueTask<Models.Foundations.Usages.AgentUsage> measureTask =
            this.usageService.MeasureAsync(prompt: CreateRandomString(), completion: null!);

        UsageValidationException actualException =
            await Assert.ThrowsAsync<UsageValidationException>(measureTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedUsageValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedUsageValidationException))),
                Times.Once);

        this.usageBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    // A custom counter is host code, so a negative count is reachable — and a negative count
    // SUBTRACTS from the run's spend, which turns a budget into something an unusual reply can
    // extend rather than exhaust.
    [Fact]
    public async Task ShouldThrowValidationExceptionOnMeasureIfCountIsNegativeAsync()
    {
        // given
        var invalidUsageException =
            new InvalidUsageException(
                message: "Invalid usage, token count cannot be negative. "
                    + "Please correct the error and try again.");

        var expectedUsageValidationException =
            new UsageValidationException(
                message: "Usage validation error occurred, fix the error and try again.",
                innerException: invalidUsageException);

        this.usageBrokerMock.Setup(broker =>
            broker.CountTokensAsync(It.IsAny<string>()))
                .ReturnsAsync(-1);

        // when
        ValueTask<Models.Foundations.Usages.AgentUsage> measureTask =
            this.usageService.MeasureAsync(CreateRandomString(), CreateRandomString());

        UsageValidationException actualException =
            await Assert.ThrowsAsync<UsageValidationException>(measureTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedUsageValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedUsageValidationException))),
                Times.Once);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
