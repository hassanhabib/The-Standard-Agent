// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using RESTFulSense.Exceptions;
using Standard.Agents.Models.Foundations.Gates.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Gates;

public partial class GateServiceTests
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
    public async Task ShouldThrowCriticalDependencyExceptionOnScreenIfCriticalErrorOccursAndLogItAsync(
        Exception criticalDependencyException)
    {
        // given
        string randomInput = CreateRandomString();

        var failedGateDependencyException =
            new FailedGateDependencyException(
                message: "Failed gate dependency error occurred, contact support.",
                innerException: criticalDependencyException);

        var expectedGateDependencyException =
            new GateDependencyException(
                message: "Gate dependency error occurred, contact support.",
                innerException: failedGateDependencyException);

        this.classifierBrokerMock.Setup(broker =>
            broker.ClassifyAsync(randomInput))
                .ThrowsAsync(criticalDependencyException);

        // when
        ValueTask<string> screenTask =
            this.gateService.ScreenAsync(randomInput);

        GateDependencyException actualGateDependencyException =
            await Assert.ThrowsAsync<GateDependencyException>(
                screenTask.AsTask);

        // then
        actualGateDependencyException.Should()
            .BeEquivalentTo(expectedGateDependencyException);

        this.classifierBrokerMock.Verify(broker =>
            broker.ClassifyAsync(randomInput),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(
                expectedGateDependencyException))),
                    Times.Once);

        this.classifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(DependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnScreenIfDependencyErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        string randomInput = CreateRandomString();

        var failedGateDependencyException =
            new FailedGateDependencyException(
                message: "Failed gate dependency error occurred, contact support.",
                innerException: dependencyException);

        var expectedGateDependencyException =
            new GateDependencyException(
                message: "Gate dependency error occurred, contact support.",
                innerException: failedGateDependencyException);

        this.classifierBrokerMock.Setup(broker =>
            broker.ClassifyAsync(randomInput))
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<string> screenTask =
            this.gateService.ScreenAsync(randomInput);

        GateDependencyException actualGateDependencyException =
            await Assert.ThrowsAsync<GateDependencyException>(
                screenTask.AsTask);

        // then
        actualGateDependencyException.Should()
            .BeEquivalentTo(expectedGateDependencyException);

        this.classifierBrokerMock.Verify(broker =>
            broker.ClassifyAsync(randomInput),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedGateDependencyException))),
                    Times.Once);

        this.classifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnScreenIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        string randomInput = CreateRandomString();
        var badRequestException = new HttpResponseBadRequestException();

        var invalidGateException =
            new InvalidGateException(
                message: "Invalid gate request. Please correct the error and try again.");

        var expectedGateDependencyValidationException =
            new GateDependencyValidationException(
                message: "Gate dependency validation error occurred, fix the error and try again.",
                innerException: invalidGateException);

        this.classifierBrokerMock.Setup(broker =>
            broker.ClassifyAsync(randomInput))
                .ThrowsAsync(badRequestException);

        // when
        ValueTask<string> screenTask =
            this.gateService.ScreenAsync(randomInput);

        GateDependencyValidationException actualGateDependencyValidationException =
            await Assert.ThrowsAsync<GateDependencyValidationException>(
                screenTask.AsTask);

        // then
        actualGateDependencyValidationException.Should()
            .BeEquivalentTo(expectedGateDependencyValidationException);

        this.classifierBrokerMock.Verify(broker =>
            broker.ClassifyAsync(randomInput),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedGateDependencyValidationException))),
                    Times.Once);

        this.classifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnScreenIfServiceErrorOccursAndLogItAsync()
    {
        // given
        string randomInput = CreateRandomString();
        var serviceException = new Exception();

        var failedGateServiceException =
            new FailedGateServiceException(
                message: "Failed gate service error occurred, contact support.",
                innerException: serviceException);

        var expectedGateServiceException =
            new GateServiceException(
                message: "Gate service error occurred, contact support.",
                innerException: failedGateServiceException);

        this.classifierBrokerMock.Setup(broker =>
            broker.ClassifyAsync(randomInput))
                .ThrowsAsync(serviceException);

        // when
        ValueTask<string> screenTask =
            this.gateService.ScreenAsync(randomInput);

        GateServiceException actualGateServiceException =
            await Assert.ThrowsAsync<GateServiceException>(
                screenTask.AsTask);

        // then
        actualGateServiceException.Should()
            .BeEquivalentTo(expectedGateServiceException);

        this.classifierBrokerMock.Verify(broker =>
            broker.ClassifyAsync(randomInput),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedGateServiceException))),
                    Times.Once);

        this.classifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
