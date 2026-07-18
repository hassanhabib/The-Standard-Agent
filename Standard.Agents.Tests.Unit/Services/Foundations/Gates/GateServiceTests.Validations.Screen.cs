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
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnScreenIfInputIsInvalidAndLogItAsync(
        string? invalidInput)
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
        ValueTask<string> screenTask =
            this.gateService.ScreenAsync(invalidInput!);

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
            broker.ClassifyAsync(It.IsAny<string>()),
                Times.Never);

        this.classifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
