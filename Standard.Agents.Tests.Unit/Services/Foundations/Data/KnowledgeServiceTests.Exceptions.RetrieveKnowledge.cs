// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Knowledges.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Data;

public partial class KnowledgeServiceTests
{
        public static TheoryData<Exception> CriticalDependencyExceptions() =>
        new()
        {
            new FileNotFoundException(),
            new DirectoryNotFoundException(),
            new UnauthorizedAccessException()
        };

    [Theory]
    [MemberData(nameof(CriticalDependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnRetrieveKnowledgeIfCriticalErrorOccursAndLogItAsync(
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

        this.knowledgeBrokerMock.Setup(broker =>
            broker.SelectKnowledgeAsync(randomQuery))
                .ThrowsAsync(criticalDependencyException);

        // when
        ValueTask<IReadOnlyList<string>> retrieveTask =
            this.knowledgeService.RetrieveKnowledgeAsync(randomQuery);

        KnowledgeDependencyException actualKnowledgeDependencyException =
            await Assert.ThrowsAsync<KnowledgeDependencyException>(
                retrieveTask.AsTask);

        // then
        actualKnowledgeDependencyException.Should()
            .BeEquivalentTo(expectedKnowledgeDependencyException);

        this.knowledgeBrokerMock.Verify(broker =>
            broker.SelectKnowledgeAsync(randomQuery),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(
                expectedKnowledgeDependencyException))),
                    Times.Once);

        this.knowledgeBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyExceptionOnRetrieveKnowledgeIfIOErrorOccursAndLogItAsync()
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

        this.knowledgeBrokerMock.Setup(broker =>
            broker.SelectKnowledgeAsync(randomQuery))
                .ThrowsAsync(ioException);

        // when
        ValueTask<IReadOnlyList<string>> retrieveTask =
            this.knowledgeService.RetrieveKnowledgeAsync(randomQuery);

        KnowledgeDependencyException actualKnowledgeDependencyException =
            await Assert.ThrowsAsync<KnowledgeDependencyException>(
                retrieveTask.AsTask);

        // then
        actualKnowledgeDependencyException.Should()
            .BeEquivalentTo(expectedKnowledgeDependencyException);

        this.knowledgeBrokerMock.Verify(broker =>
            broker.SelectKnowledgeAsync(randomQuery),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedKnowledgeDependencyException))),
                    Times.Once);

        this.knowledgeBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnRetrieveKnowledgeIfServiceErrorOccursAndLogItAsync()
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

        this.knowledgeBrokerMock.Setup(broker =>
            broker.SelectKnowledgeAsync(randomQuery))
                .ThrowsAsync(serviceException);

        // when
        ValueTask<IReadOnlyList<string>> retrieveTask =
            this.knowledgeService.RetrieveKnowledgeAsync(randomQuery);

        KnowledgeServiceException actualKnowledgeServiceException =
            await Assert.ThrowsAsync<KnowledgeServiceException>(
                retrieveTask.AsTask);

        // then
        actualKnowledgeServiceException.Should()
            .BeEquivalentTo(expectedKnowledgeServiceException);

        this.knowledgeBrokerMock.Verify(broker =>
            broker.SelectKnowledgeAsync(randomQuery),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedKnowledgeServiceException))),
                    Times.Once);

        this.knowledgeBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

            }
