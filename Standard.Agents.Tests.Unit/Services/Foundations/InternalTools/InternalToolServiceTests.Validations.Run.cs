// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.InternalTools.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.InternalTools;

public partial class InternalToolServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnRunIfNameIsInvalidAndLogItAsync(
        string? invalidName)
    {
        // given
        string randomInput = CreateRandomString();

        var invalidInternalToolException =
            new InvalidInternalToolException(
                message: "Invalid internal tool. Please correct the error and try again.");

        var expectedInternalToolValidationException =
            new InternalToolValidationException(
                message: "Internal tool validation error occurred, fix the error and try again.",
                innerException: invalidInternalToolException);

        // when
        ValueTask<string> runTask =
            this.internalToolService.RunAsync(invalidName!, randomInput);

        InternalToolValidationException actualInternalToolValidationException =
            await Assert.ThrowsAsync<InternalToolValidationException>(
                runTask.AsTask);

        // then
        actualInternalToolValidationException.Should()
            .BeEquivalentTo(expectedInternalToolValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedInternalToolValidationException))),
                    Times.Once);

        this.toolBrokerMock.Verify(broker =>
            broker.RunAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

        this.toolBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldRunOnRunEvenIfInputIsEmptyAsync(string? emptyInput)
    {
        // given
        string randomName = CreateRandomString();
        string randomOutput = CreateRandomString();
        string inputName = randomName;
        string expectedOutput = randomOutput;

        this.toolBrokerMock.Setup(broker =>
            broker.RunAsync(inputName, emptyInput!))
                .ReturnsAsync(randomOutput);

        // when
        string actualOutput =
            await this.internalToolService.RunAsync(inputName, emptyInput!);

        // then
        actualOutput.Should().BeEquivalentTo(expectedOutput);

        this.toolBrokerMock.Verify(broker =>
            broker.RunAsync(inputName, emptyInput!),
                Times.Once);

        this.toolBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
    }
