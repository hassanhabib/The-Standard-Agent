// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using RESTFulSense.Exceptions;
using Standard.Agents.Models.Foundations.ExternalTools.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.ExternalTools;

public partial class ExternalToolServiceTests
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
    public async Task ShouldThrowCriticalDependencyExceptionOnCallIfCriticalErrorOccursAndLogItAsync(
Exception criticalDependencyException)
    {
        // given
        string randomName = CreateRandomString();
        string randomInput = CreateRandomString();

        var failedExternalToolDependencyException =
            new FailedExternalToolDependencyException(
                message: "Failed external tool dependency error occurred, contact support.",
                innerException: criticalDependencyException);

        var expectedExternalToolDependencyException =
            new ExternalToolDependencyException(
                message: "External tool dependency error occurred, contact support.",
                innerException: failedExternalToolDependencyException);

        this.mcpBrokerMock.Setup(broker =>
            broker.CallAsync(randomName, randomInput))
                .ThrowsAsync(criticalDependencyException);

        // when
        ValueTask<string> callTask =
            this.externalToolService.CallAsync(randomName, randomInput);

        ExternalToolDependencyException actualExternalToolDependencyException =
            await Assert.ThrowsAsync<ExternalToolDependencyException>(
                callTask.AsTask);

        // then
        actualExternalToolDependencyException.Should()
            .BeEquivalentTo(expectedExternalToolDependencyException);

        this.mcpBrokerMock.Verify(broker =>
            broker.CallAsync(randomName, randomInput),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(
                expectedExternalToolDependencyException))),
                    Times.Once);

        this.mcpBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(DependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnCallIfDependencyErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string randomName = CreateRandomString();
        string randomInput = CreateRandomString();

        var failedExternalToolDependencyException =
            new FailedExternalToolDependencyException(
                message: "Failed external tool dependency error occurred, contact support.",
                innerException: dependencyException);

        var expectedExternalToolDependencyException =
            new ExternalToolDependencyException(
                message: "External tool dependency error occurred, contact support.",
                innerException: failedExternalToolDependencyException);

        this.mcpBrokerMock.Setup(broker =>
            broker.CallAsync(randomName, randomInput))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<string> callTask =
            this.externalToolService.CallAsync(randomName, randomInput);

        ExternalToolDependencyException actualExternalToolDependencyException =
            await Assert.ThrowsAsync<ExternalToolDependencyException>(
                callTask.AsTask);

        // then
        actualExternalToolDependencyException.Should()
            .BeEquivalentTo(expectedExternalToolDependencyException);

        this.mcpBrokerMock.Verify(broker =>
            broker.CallAsync(randomName, randomInput),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedExternalToolDependencyException))),
                    Times.Once);

        this.mcpBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnCallIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        string randomName = CreateRandomString();
        string randomInput = CreateRandomString();
        var badRequestException = new HttpResponseBadRequestException();

        var invalidExternalToolException =
            new InvalidExternalToolException(
                message: "Invalid external tool request. Please correct the error and try again.");

        var expectedExternalToolDependencyValidationException =
            new ExternalToolDependencyValidationException(
                message: "External tool dependency validation error occurred, fix the error and try again.",
                innerException: invalidExternalToolException);

        this.mcpBrokerMock.Setup(broker =>
            broker.CallAsync(randomName, randomInput))
                .ThrowsAsync(badRequestException);

        // when
        ValueTask<string> callTask =
            this.externalToolService.CallAsync(randomName, randomInput);

        ExternalToolDependencyValidationException actualExternalToolDependencyValidationException =
            await Assert.ThrowsAsync<ExternalToolDependencyValidationException>(
                callTask.AsTask);

        // then
        actualExternalToolDependencyValidationException.Should()
            .BeEquivalentTo(expectedExternalToolDependencyValidationException);

        this.mcpBrokerMock.Verify(broker =>
            broker.CallAsync(randomName, randomInput),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedExternalToolDependencyValidationException))),
                    Times.Once);

        this.mcpBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnCallIfServiceErrorOccursAndLogItAsync()
    {
        // given
        string randomName = CreateRandomString();
        string randomInput = CreateRandomString();
        var serviceException = new Exception();

        var failedExternalToolServiceException =
            new FailedExternalToolServiceException(
                message: "Failed external tool service error occurred, contact support.",
                innerException: serviceException);

        var expectedExternalToolServiceException =
            new ExternalToolServiceException(
                message: "External tool service error occurred, contact support.",
                innerException: failedExternalToolServiceException);

        this.mcpBrokerMock.Setup(broker =>
            broker.CallAsync(randomName, randomInput))
                .ThrowsAsync(serviceException);

        // when
        ValueTask<string> callTask =
            this.externalToolService.CallAsync(randomName, randomInput);

        ExternalToolServiceException actualExternalToolServiceException =
            await Assert.ThrowsAsync<ExternalToolServiceException>(
                callTask.AsTask);

        // then
        actualExternalToolServiceException.Should()
            .BeEquivalentTo(expectedExternalToolServiceException);

        this.mcpBrokerMock.Verify(broker =>
            broker.CallAsync(randomName, randomInput),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedExternalToolServiceException))),
                    Times.Once);

        this.mcpBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
    }
