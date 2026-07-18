// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Threading;
using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Brains.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Brains;

public partial class BrainServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnGenerateStreamIfPromptIsInvalidAndLogItAsync(
        string? invalidUserPrompt)
    {
        // given
        string randomSystemPrompt = CreateRandomString();

        var invalidBrainException =
            new InvalidBrainException(
                message: "Invalid brain input. Please correct the error and try again.");

        var expectedBrainValidationException =
            new BrainValidationException(
                message: "Brain validation error occurred, fix the error and try again.",
                innerException: invalidBrainException);

        // when
        BrainValidationException actualBrainValidationException =
            await Assert.ThrowsAsync<BrainValidationException>(() =>
                DrainAsync(this.brainService.GenerateStreamAsync(
                    randomSystemPrompt, invalidUserPrompt!)));

        // then
        actualBrainValidationException.Should()
            .BeEquivalentTo(expectedBrainValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedBrainValidationException))),
                    Times.Once);

        this.generatorBrokerMock.Verify(broker =>
            broker.GenerateStreamAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Never);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnGenerateStreamIfHttpRequestErrorOccursAndLogItAsync()
    {
        // given
        string randomSystemPrompt = CreateRandomString();
        string randomUserPrompt = CreateRandomString();
        var httpRequestException = new HttpRequestException();

        var failedBrainDependencyException =
            new FailedBrainDependencyException(
                message: "Failed brain dependency error occurred, contact support.",
                innerException: httpRequestException);

        var expectedBrainDependencyException =
            new BrainDependencyException(
                message: "Brain dependency error occurred, contact support.",
                innerException: failedBrainDependencyException);

        this.generatorBrokerMock.Setup(broker =>
            broker.GenerateStreamAsync(
                randomSystemPrompt, randomUserPrompt, It.IsAny<CancellationToken>()))
                    .Returns(ThrowingStream(httpRequestException));

        // when
        BrainDependencyException actualBrainDependencyException =
            await Assert.ThrowsAsync<BrainDependencyException>(() =>
                DrainAsync(this.brainService.GenerateStreamAsync(
                    randomSystemPrompt, randomUserPrompt)));

        // then
        actualBrainDependencyException.Should()
            .BeEquivalentTo(expectedBrainDependencyException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(
                expectedBrainDependencyException))),
                    Times.Once);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnGenerateStreamIfServiceErrorOccursAndLogItAsync()
    {
        // given
        string randomSystemPrompt = CreateRandomString();
        string randomUserPrompt = CreateRandomString();
        var serviceException = new Exception();

        var failedBrainServiceException =
            new FailedBrainServiceException(
                message: "Failed brain service error occurred, contact support.",
                innerException: serviceException);

        var expectedBrainServiceException =
            new BrainServiceException(
                message: "Brain service error occurred, contact support.",
                innerException: failedBrainServiceException);

        this.generatorBrokerMock.Setup(broker =>
            broker.GenerateStreamAsync(
                randomSystemPrompt, randomUserPrompt, It.IsAny<CancellationToken>()))
                    .Returns(ThrowingStream(serviceException));

        // when
        BrainServiceException actualBrainServiceException =
            await Assert.ThrowsAsync<BrainServiceException>(() =>
                DrainAsync(this.brainService.GenerateStreamAsync(
                    randomSystemPrompt, randomUserPrompt)));

        // then
        actualBrainServiceException.Should()
            .BeEquivalentTo(expectedBrainServiceException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedBrainServiceException))),
                    Times.Once);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
    }
