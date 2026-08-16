// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Gates.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Gates;

public partial class GateServiceTests
{
    [Fact]
    public async Task ShouldDetectConflictAsync()
    {
        // given
        string randomInstructions = CreateRandomString();
        string randomVerdict = CreateRandomString();

        this.classifierBrokerMock.Setup(broker =>
            broker.AssessAsync(It.IsAny<string>(), randomInstructions))
                .ReturnsAsync(randomVerdict);

        // when
        string actualVerdict =
            await this.gateService.DetectConflictAsync(randomInstructions);

        // then
        actualVerdict.Should().BeEquivalentTo(randomVerdict);

        this.classifierBrokerMock.Verify(broker =>
            broker.AssessAsync(It.IsAny<string>(), randomInstructions),
                Times.Once);

        this.classifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ShouldThrowValidationExceptionOnDetectConflictIfInstructionsInvalidAndLogItAsync(
        string? invalidInstructions)
    {
        // given
        var invalidGateException =
            new InvalidGateException(
                message: "Invalid gate input. Please correct the error and try again.");

        var expectedGateValidationException =
            new GateValidationException(
                message: "Gate validation error occurred, fix the error and try again.",
                innerException: invalidGateException);

        // when
        ValueTask<string> detectConflictTask =
            this.gateService.DetectConflictAsync(invalidInstructions!);

        GateValidationException actualGateValidationException =
            await Assert.ThrowsAsync<GateValidationException>(detectConflictTask.AsTask);

        // then
        actualGateValidationException.Should().BeEquivalentTo(expectedGateValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedGateValidationException))),
                Times.Once);

        this.classifierBrokerMock.Verify(broker =>
            broker.AssessAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

        this.classifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
