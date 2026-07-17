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
                [Fact]
    public async Task ShouldThrowDependencyExceptionOnRunIfToolErrorOccursAndLogItAsync()
    {
        // given
        string randomName = CreateRandomString();
        string randomInput = CreateRandomString();
        string inputName = randomName;
        string inputInput = randomInput;
        var toolException = new InvalidOperationException();

        var failedInternalToolDependencyException =
            new FailedInternalToolDependencyException(
                message: "Failed internal tool dependency error occurred, contact support.",
                innerException: toolException);

        var expectedInternalToolDependencyException =
            new InternalToolDependencyException(
                message: "Internal tool dependency error occurred, contact support.",
                innerException: failedInternalToolDependencyException);

        this.toolBrokerMock.Setup(broker =>
            broker.RunAsync(inputName, inputInput))
                .ThrowsAsync(toolException);

        // when
        ValueTask<string> runTask =
            this.internalToolService.RunAsync(inputName, inputInput);

        InternalToolDependencyException actualInternalToolDependencyException =
            await Assert.ThrowsAsync<InternalToolDependencyException>(
                runTask.AsTask);

        // then
        actualInternalToolDependencyException.Should()
            .BeEquivalentTo(expectedInternalToolDependencyException);

        this.toolBrokerMock.Verify(broker =>
            broker.RunAsync(inputName, inputInput),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedInternalToolDependencyException))),
                    Times.Once);

        this.toolBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

                    [Fact]
    public async Task ShouldThrowDependencyExceptionOnRunIfToolIsNotFoundAndLogItAsync()
    {
        // given
        string randomName = CreateRandomString();
        string randomInput = CreateRandomString();
        string inputName = randomName;
        string inputInput = randomInput;
        var keyNotFoundException = new KeyNotFoundException();

        var failedInternalToolDependencyException =
            new FailedInternalToolDependencyException(
                message: "Failed internal tool dependency error occurred, contact support.",
                innerException: keyNotFoundException);

        var expectedInternalToolDependencyException =
            new InternalToolDependencyException(
                message: "Internal tool dependency error occurred, contact support.",
                innerException: failedInternalToolDependencyException);

        this.toolBrokerMock.Setup(broker =>
            broker.RunAsync(inputName, inputInput))
                .ThrowsAsync(keyNotFoundException);

        // when
        ValueTask<string> runTask =
            this.internalToolService.RunAsync(inputName, inputInput);

        InternalToolDependencyException actualInternalToolDependencyException =
            await Assert.ThrowsAsync<InternalToolDependencyException>(
                runTask.AsTask);

        // then
        actualInternalToolDependencyException.Should()
            .BeEquivalentTo(expectedInternalToolDependencyException);

        this.toolBrokerMock.Verify(broker =>
            broker.RunAsync(inputName, inputInput),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedInternalToolDependencyException))),
                    Times.Once);

        this.toolBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
