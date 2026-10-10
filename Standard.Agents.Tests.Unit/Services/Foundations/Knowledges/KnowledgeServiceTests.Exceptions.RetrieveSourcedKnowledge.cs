// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Knowledges;
using Standard.Agents.Models.Foundations.Knowledges.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Knowledges;

public partial class KnowledgeServiceTests
{
    [Theory]
    [MemberData(nameof(CriticalDependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnRetrieveSourcedKnowledgeIfCriticalErrorOccursAndLogItAsync(
        Exception criticalDependencyException)
    {
        // given
        string randomQuery = CreateRandomString();

        var failedKnowledgeDependencyException =
            new FailedKnowledgeDependencyException(
                message: "Failed knowledge dependency error occurred, contact support.",
                innerException: criticalDependencyException);

        var expectedKnowledgeDependencyException =
            new KnowledgeDependencyException(
                message: "Knowledge dependency error occurred, contact support.",
                innerException: failedKnowledgeDependencyException);

        this.sourcedKnowledgeBrokerMock.Setup(broker =>
            broker.SelectSourcedKnowledgeAsync(randomQuery))
                .ThrowsAsync(criticalDependencyException);

        // when
        ValueTask<IReadOnlyList<KnowledgeResult>> retrieveTask =
            this.sourcedKnowledgeService.RetrieveSourcedKnowledgeAsync(randomQuery);

        KnowledgeDependencyException actualKnowledgeDependencyException =
            await Assert.ThrowsAsync<KnowledgeDependencyException>(
                retrieveTask.AsTask);

        // then
        actualKnowledgeDependencyException.Should()
            .BeEquivalentTo(expectedKnowledgeDependencyException);

        this.sourcedKnowledgeBrokerMock.Verify(broker =>
            broker.SelectSourcedKnowledgeAsync(randomQuery),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(
                expectedKnowledgeDependencyException))),
                    Times.Once);

        this.sourcedKnowledgeBrokerMock.VerifyNoOtherCalls();
        this.knowledgeBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyExceptionOnRetrieveSourcedKnowledgeIfIOErrorOccursAndLogItAsync()
    {
        // given
        string randomQuery = CreateRandomString();
        var ioException = new IOException();

        var failedKnowledgeDependencyException =
            new FailedKnowledgeDependencyException(
                message: "Failed knowledge dependency error occurred, contact support.",
                innerException: ioException);

        var expectedKnowledgeDependencyException =
            new KnowledgeDependencyException(
                message: "Knowledge dependency error occurred, contact support.",
                innerException: failedKnowledgeDependencyException);

        this.sourcedKnowledgeBrokerMock.Setup(broker =>
            broker.SelectSourcedKnowledgeAsync(randomQuery))
                .ThrowsAsync(ioException);

        // when
        ValueTask<IReadOnlyList<KnowledgeResult>> retrieveTask =
            this.sourcedKnowledgeService.RetrieveSourcedKnowledgeAsync(randomQuery);

        KnowledgeDependencyException actualKnowledgeDependencyException =
            await Assert.ThrowsAsync<KnowledgeDependencyException>(
                retrieveTask.AsTask);

        // then
        actualKnowledgeDependencyException.Should()
            .BeEquivalentTo(expectedKnowledgeDependencyException);

        this.sourcedKnowledgeBrokerMock.Verify(broker =>
            broker.SelectSourcedKnowledgeAsync(randomQuery),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedKnowledgeDependencyException))),
                    Times.Once);

        this.sourcedKnowledgeBrokerMock.VerifyNoOtherCalls();
        this.knowledgeBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnRetrieveSourcedKnowledgeIfServiceErrorOccursAndLogItAsync()
    {
        // given
        string randomQuery = CreateRandomString();
        var serviceException = new Exception();

        var failedKnowledgeServiceException =
            new FailedKnowledgeServiceException(
                message: "Failed knowledge service error occurred, contact support.",
                innerException: serviceException);

        var expectedKnowledgeServiceException =
            new KnowledgeServiceException(
                message: "Knowledge service error occurred, contact support.",
                innerException: failedKnowledgeServiceException);

        this.sourcedKnowledgeBrokerMock.Setup(broker =>
            broker.SelectSourcedKnowledgeAsync(randomQuery))
                .ThrowsAsync(serviceException);

        // when
        ValueTask<IReadOnlyList<KnowledgeResult>> retrieveTask =
            this.sourcedKnowledgeService.RetrieveSourcedKnowledgeAsync(randomQuery);

        KnowledgeServiceException actualKnowledgeServiceException =
            await Assert.ThrowsAsync<KnowledgeServiceException>(
                retrieveTask.AsTask);

        // then
        actualKnowledgeServiceException.Should()
            .BeEquivalentTo(expectedKnowledgeServiceException);

        this.sourcedKnowledgeBrokerMock.Verify(broker =>
            broker.SelectSourcedKnowledgeAsync(randomQuery),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedKnowledgeServiceException))),
                    Times.Once);

        this.sourcedKnowledgeBrokerMock.VerifyNoOtherCalls();
        this.knowledgeBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

}
