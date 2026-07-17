// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.InternalTools.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Direction;

public partial class InternalToolServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnHandlesIfNameIsInvalidAndLogItAsync(
        string? invalidName)
    {
        // given
        var invalidInternalToolException =
            new InvalidInternalToolException(
                message: "Invalid internal tool. Please correct the error and try again.");

        var expectedInternalToolValidationException =
            new InternalToolValidationException(
                message: "Internal tool validation error occurred, fix the error and try again.",
                innerException: invalidInternalToolException);

        // when
        ValueTask<bool> handlesTask =
            this.internalToolService.HandlesAsync(invalidName!);

        InternalToolValidationException actualInternalToolValidationException =
            await Assert.ThrowsAsync<InternalToolValidationException>(
                handlesTask.AsTask);

        // then
        actualInternalToolValidationException.Should()
            .BeEquivalentTo(expectedInternalToolValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedInternalToolValidationException))),
                    Times.Once);

                this.toolBrokerMock.Verify(broker =>
            broker.HasAsync(It.IsAny<string>()),
                Times.Never);

        this.toolBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
