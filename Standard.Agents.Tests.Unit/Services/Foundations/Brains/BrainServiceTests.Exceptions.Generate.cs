// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using RESTFulSense.Exceptions;
using Standard.Agents.Models.Foundations.Brains.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Brains;

public partial class BrainServiceTests
{
    public static TheoryData<Exception> CriticalDependencyExceptions() =>
        new()
        {
            new HttpResponseUnauthorizedException(),
            new HttpResponseForbiddenException(),
            new HttpResponseNotFoundException(),
            new HttpResponseUrlNotFoundException(),
            new HttpRequestException()
        };

    public static TheoryData<Exception> DependencyExceptions() =>
        new()
        {
            new HttpResponseInternalServerErrorException(),
            new HttpResponseServiceUnavailableException()
        };

    [Theory]
    [MemberData(nameof(CriticalDependencyExceptions))]
    public async Task ShouldThrowCriticalDependencyExceptionOnGenerateIfCriticalErrorOccursAndLogItAsync(
        Exception criticalDependencyException)
    {
        // given
        string randomSystemPrompt = CreateRandomString();
        string randomUserPrompt = CreateRandomString();

        var failedBrainDependencyException =
            new FailedBrainDependencyException(
                message: "Failed brain dependency error occurred, contact support.",
                innerException: criticalDependencyException);

        var expectedBrainDependencyException =
            new BrainDependencyException(
                message: "Brain dependency error occurred, contact support.",
                innerException: failedBrainDependencyException);

        this.generatorBrokerMock.Setup(broker =>
            broker.GenerateAsync(randomSystemPrompt, randomUserPrompt))
                .ThrowsAsync(criticalDependencyException);

        // when
        ValueTask<string> generateTask =
            this.brainService.GenerateAsync(randomSystemPrompt, randomUserPrompt);

        BrainDependencyException actualBrainDependencyException =
            await Assert.ThrowsAsync<BrainDependencyException>(
                generateTask.AsTask);

        // then
        actualBrainDependencyException.Should()
            .BeEquivalentTo(expectedBrainDependencyException);

        this.generatorBrokerMock.Verify(broker =>
            broker.GenerateAsync(randomSystemPrompt, randomUserPrompt),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(
                expectedBrainDependencyException))),
                    Times.Once);

        this.generatorBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(DependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnGenerateIfDependencyErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string randomSystemPrompt = CreateRandomString();
        string randomUserPrompt = CreateRandomString();

        var failedBrainDependencyException =
            new FailedBrainDependencyException(
                message: "Failed brain dependency error occurred, contact support.",
                innerException: dependencyException);

        var expectedBrainDependencyException =
            new BrainDependencyException(
                message: "Brain dependency error occurred, contact support.",
                innerException: failedBrainDependencyException);

        this.generatorBrokerMock.Setup(broker =>
            broker.GenerateAsync(randomSystemPrompt, randomUserPrompt))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<string> generateTask =
            this.brainService.GenerateAsync(randomSystemPrompt, randomUserPrompt);

        BrainDependencyException actualBrainDependencyException =
            await Assert.ThrowsAsync<BrainDependencyException>(
                generateTask.AsTask);

        // then
        actualBrainDependencyException.Should()
            .BeEquivalentTo(expectedBrainDependencyException);

        this.generatorBrokerMock.Verify(broker =>
            broker.GenerateAsync(randomSystemPrompt, randomUserPrompt),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedBrainDependencyException))),
                    Times.Once);

        this.generatorBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnGenerateIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        string randomSystemPrompt = CreateRandomString();
        string randomUserPrompt = CreateRandomString();
        var badRequestException = new HttpResponseBadRequestException();

        var invalidBrainException =
            new InvalidBrainException(
                message: "Invalid brain request. Please correct the error and try again.");

        var expectedBrainDependencyValidationException =
            new BrainDependencyValidationException(
                message: "Brain dependency validation error occurred, fix the error and try again.",
                innerException: invalidBrainException);

        this.generatorBrokerMock.Setup(broker =>
            broker.GenerateAsync(randomSystemPrompt, randomUserPrompt))
                .ThrowsAsync(badRequestException);

        // when
        ValueTask<string> generateTask =
            this.brainService.GenerateAsync(randomSystemPrompt, randomUserPrompt);

        BrainDependencyValidationException actualBrainDependencyValidationException =
            await Assert.ThrowsAsync<BrainDependencyValidationException>(
                generateTask.AsTask);

        // then
        actualBrainDependencyValidationException.Should()
            .BeEquivalentTo(expectedBrainDependencyValidationException);

        this.generatorBrokerMock.Verify(broker =>
            broker.GenerateAsync(randomSystemPrompt, randomUserPrompt),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedBrainDependencyValidationException))),
                    Times.Once);

        this.generatorBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnGenerateIfServiceErrorOccursAndLogItAsync()
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
            broker.GenerateAsync(randomSystemPrompt, randomUserPrompt))
                .ThrowsAsync(serviceException);

        // when
        ValueTask<string> generateTask =
            this.brainService.GenerateAsync(randomSystemPrompt, randomUserPrompt);

        BrainServiceException actualBrainServiceException =
            await Assert.ThrowsAsync<BrainServiceException>(
                generateTask.AsTask);

        // then
        actualBrainServiceException.Should()
            .BeEquivalentTo(expectedBrainServiceException);

        this.generatorBrokerMock.Verify(broker =>
            broker.GenerateAsync(randomSystemPrompt, randomUserPrompt),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedBrainServiceException))),
                    Times.Once);

        this.generatorBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
