// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Returns.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Direction;

public partial class ReturnServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnReturnIfPayloadIsInvalidAndLogItAsync(
        string? invalidPayload)
    {
        // given
        var invalidReturnException =
            new InvalidReturnException(
                message: "Invalid return payload. Please correct the error and try again.");

        var expectedReturnValidationException =
            new ReturnValidationException(
                message: "Return validation error occurred, fix the error and try again.",
                innerException: invalidReturnException);

        // when
        ValueTask<string> returnTask =
            this.returnService.ReturnAsync(invalidPayload!);

        ReturnValidationException actualReturnValidationException =
            await Assert.ThrowsAsync<ReturnValidationException>(
                returnTask.AsTask);

        // then
        actualReturnValidationException.Should()
            .BeEquivalentTo(expectedReturnValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedReturnValidationException))),
                    Times.Once);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
