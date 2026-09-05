// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using RESTFulSense.Exceptions;
using Standard.Agents.Models.Brokers.Mcps;
using Standard.Agents.Models.Foundations.ExternalTools.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.ExternalTools;

public partial class ExternalToolServiceTests
{
    [Theory]
    [MemberData(nameof(CriticalDependencyExceptions))]
    public async Task ShouldThrowCriticalDependencyExceptionOnRetrieveToolsIfCriticalErrorOccursAndLogItAsync(
        Exception criticalDependencyException)
    {
        // given
        var failedExternalToolDependencyException =
            new FailedExternalToolDependencyException(
                message: "Failed external tool dependency error occurred, contact support.",
                innerException: criticalDependencyException);

        var expectedExternalToolDependencyException =
            new ExternalToolDependencyException(
                message: "External tool dependency error occurred, contact support.",
                innerException: failedExternalToolDependencyException);

        this.mcpBrokerMock.Setup(broker =>
            broker.ListToolsAsync())
                .ThrowsAsync(criticalDependencyException);

        // when
        ValueTask<IReadOnlyList<McpTool>> retrieveToolsTask =
            this.externalToolService.RetrieveToolsAsync();

        ExternalToolDependencyException actualExternalToolDependencyException =
            await Assert.ThrowsAsync<ExternalToolDependencyException>(
                retrieveToolsTask.AsTask);

        // then
        actualExternalToolDependencyException.Should()
            .BeEquivalentTo(expectedExternalToolDependencyException);

        this.mcpBrokerMock.Verify(broker =>
            broker.ListToolsAsync(),
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
    public async Task ShouldThrowDependencyExceptionOnRetrieveToolsIfDependencyErrorOccursAndLogItAsync(
        Exception dependencyException)
    {
        // given
        var failedExternalToolDependencyException =
            new FailedExternalToolDependencyException(
                message: "Failed external tool dependency error occurred, contact support.",
                innerException: dependencyException);

        var expectedExternalToolDependencyException =
            new ExternalToolDependencyException(
                message: "External tool dependency error occurred, contact support.",
                innerException: failedExternalToolDependencyException);

        this.mcpBrokerMock.Setup(broker =>
            broker.ListToolsAsync())
                .ThrowsAsync(dependencyException);

        // when
        ValueTask<IReadOnlyList<McpTool>> retrieveToolsTask =
            this.externalToolService.RetrieveToolsAsync();

        ExternalToolDependencyException actualExternalToolDependencyException =
            await Assert.ThrowsAsync<ExternalToolDependencyException>(
                retrieveToolsTask.AsTask);

        // then
        actualExternalToolDependencyException.Should()
            .BeEquivalentTo(expectedExternalToolDependencyException);

        this.mcpBrokerMock.Verify(broker =>
            broker.ListToolsAsync(),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedExternalToolDependencyException))),
                    Times.Once);

        this.mcpBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyValidationExceptionOnRetrieveToolsIfBadRequestErrorOccursAndLogItAsync()
    {
        // given
        var httpResponseBadRequestException = new HttpResponseBadRequestException();

        var invalidExternalToolException =
            new InvalidExternalToolException(
                message: "Invalid external tool request. Please correct the error and try again.");

        invalidExternalToolException.AddData(httpResponseBadRequestException.Data);

        var expectedExternalToolDependencyValidationException =
            new ExternalToolDependencyValidationException(
                message: "External tool dependency validation error occurred, fix the error and try again.",
                innerException: invalidExternalToolException);

        this.mcpBrokerMock.Setup(broker =>
            broker.ListToolsAsync())
                .ThrowsAsync(httpResponseBadRequestException);

        // when
        ValueTask<IReadOnlyList<McpTool>> retrieveToolsTask =
            this.externalToolService.RetrieveToolsAsync();

        ExternalToolDependencyValidationException actualExternalToolDependencyValidationException =
            await Assert.ThrowsAsync<ExternalToolDependencyValidationException>(
                retrieveToolsTask.AsTask);

        // then
        actualExternalToolDependencyValidationException.Should()
            .BeEquivalentTo(expectedExternalToolDependencyValidationException);

        this.mcpBrokerMock.Verify(broker =>
            broker.ListToolsAsync(),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedExternalToolDependencyValidationException))),
                    Times.Once);

        this.mcpBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnRetrieveToolsIfServiceErrorOccursAndLogItAsync()
    {
        // given
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
            broker.ListToolsAsync())
                .ThrowsAsync(serviceException);

        // when
        ValueTask<IReadOnlyList<McpTool>> retrieveToolsTask =
            this.externalToolService.RetrieveToolsAsync();

        ExternalToolServiceException actualExternalToolServiceException =
            await Assert.ThrowsAsync<ExternalToolServiceException>(
                retrieveToolsTask.AsTask);

        // then
        actualExternalToolServiceException.Should()
            .BeEquivalentTo(expectedExternalToolServiceException);

        this.mcpBrokerMock.Verify(broker =>
            broker.ListToolsAsync(),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedExternalToolServiceException))),
                    Times.Once);

        this.mcpBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
