// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.ExternalTools.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.ExternalTools;

public partial class ExternalToolServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnCallIfNameIsInvalidAndLogItAsync(
        string? invalidName)
    {
        // given
        string randomInput = CreateRandomString();

        var invalidExternalToolException =
            new InvalidExternalToolException(
                message: "Invalid external tool. Please correct the error and try again.");

        var expectedExternalToolValidationException =
            new ExternalToolValidationException(
                message: "External tool validation error occurred, fix the error and try again.",
                innerException: invalidExternalToolException);

        // when
        ValueTask<string> callTask =
            this.externalToolService.CallAsync(invalidName!, randomInput);

        ExternalToolValidationException actualExternalToolValidationException =
            await Assert.ThrowsAsync<ExternalToolValidationException>(
                callTask.AsTask);

        // then
        actualExternalToolValidationException.Should()
            .BeEquivalentTo(expectedExternalToolValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedExternalToolValidationException))),
                    Times.Once);

        this.mcpBrokerMock.Verify(broker =>
broker.CallAsync(It.IsAny<string>(), It.IsAny<string>()),
Times.Never);

        this.mcpBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
    }
