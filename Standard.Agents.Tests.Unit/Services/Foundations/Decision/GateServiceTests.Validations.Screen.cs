// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Gates.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Decision;

public partial class GateServiceTests
{
    // Both are validated, unlike Brain where the system prompt may be empty. A Gate
    // with no gatePrompt has no rules to screen against, so it cannot screen — and a
    // guardian that cannot screen must fail loudly rather than wave input through.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnScreenIfGatePromptIsInvalidAndLogItAsync(
        string invalidGatePrompt)
    {
        // given
        string randomInput = CreateRandomString();

        var invalidGateException =
            new InvalidGateException(
                message: "Invalid gate input. Please correct the error and try again.");

        var expectedGateValidationException =
            new GateValidationException(
                message: "Gate validation error occurred, fix the error and try again.",
                innerException: invalidGateException);

        // when
        ValueTask<string> screenTask =
            this.gateService.ScreenAsync(invalidGatePrompt, randomInput);

        GateValidationException actualGateValidationException =
            await Assert.ThrowsAsync<GateValidationException>(
                screenTask.AsTask);

        // then
        actualGateValidationException.Should()
            .BeEquivalentTo(expectedGateValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedGateValidationException))),
                    Times.Once);

        this.classifierBrokerMock.Verify(broker =>
            broker.ClassifyAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

        this.classifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnScreenIfInputIsInvalidAndLogItAsync(
        string invalidInput)
    {
        // given
        string randomGatePrompt = CreateRandomString();

        var invalidGateException =
            new InvalidGateException(
                message: "Invalid gate input. Please correct the error and try again.");

        var expectedGateValidationException =
            new GateValidationException(
                message: "Gate validation error occurred, fix the error and try again.",
                innerException: invalidGateException);

        // when
        ValueTask<string> screenTask =
            this.gateService.ScreenAsync(randomGatePrompt, invalidInput);

        GateValidationException actualGateValidationException =
            await Assert.ThrowsAsync<GateValidationException>(
                screenTask.AsTask);

        // then
        actualGateValidationException.Should()
            .BeEquivalentTo(expectedGateValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedGateValidationException))),
                    Times.Once);

        this.classifierBrokerMock.Verify(broker =>
            broker.ClassifyAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

        this.classifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
