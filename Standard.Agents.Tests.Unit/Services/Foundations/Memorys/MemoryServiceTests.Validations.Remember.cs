// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Memorys.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Memorys;

public partial class MemoryServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnRememberIfMemoryIsInvalidAndLogItAsync(
string? invalidMemory)
    {
        // given
        var invalidMemoryException =
            new InvalidMemoryException(
                message: "Invalid memory. Please correct the error and try again.");

        var expectedMemoryValidationException =
            new MemoryValidationException(
                message: "Memory validation error occurred, fix the error and try again.",
                innerException: invalidMemoryException);

        // when
        ValueTask rememberTask =
            this.memoryService.RememberAsync(invalidMemory!);

        MemoryValidationException actualMemoryValidationException =
            await Assert.ThrowsAsync<MemoryValidationException>(
                rememberTask.AsTask);

        // then
        actualMemoryValidationException.Should()
            .BeEquivalentTo(expectedMemoryValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedMemoryValidationException))),
                    Times.Once);

        this.memoryBrokerMock.Verify(broker =>
            broker.InsertMemoryAsync(It.IsAny<string>()),
                Times.Never);

        this.memoryBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
    }
