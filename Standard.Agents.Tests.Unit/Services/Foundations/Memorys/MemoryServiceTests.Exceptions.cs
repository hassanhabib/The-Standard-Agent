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
    public static TheoryData<Exception> CriticalDependencyExceptions() =>
    new()
    {
            new FileNotFoundException(),
            new DirectoryNotFoundException(),
            new UnauthorizedAccessException()
    };

    [Theory]
    [MemberData(nameof(CriticalDependencyExceptions))]
    public async Task ShouldThrowDependencyExceptionOnRecallMemoriesIfCriticalErrorOccursAndLogItAsync(
        Exception criticalDependencyException)
    {
        // given
        var failedMemoryDependencyException =
            new FailedMemoryDependencyException(
                message: "Failed memory dependency error occurred, contact support.",
                innerException: criticalDependencyException);

        var expectedMemoryDependencyException =
            new MemoryDependencyException(
                message: "Memory dependency error occurred, contact support.",
                innerException: failedMemoryDependencyException);

        this.memoryBrokerMock.Setup(broker =>
            broker.SelectMemoriesAsync())
                .ThrowsAsync(criticalDependencyException);

        // when
        ValueTask<IReadOnlyList<string>> recallTask =
            this.memoryService.RecallMemoriesAsync();

        MemoryDependencyException actualMemoryDependencyException =
            await Assert.ThrowsAsync<MemoryDependencyException>(
                recallTask.AsTask);

        // then
        actualMemoryDependencyException.Should()
            .BeEquivalentTo(expectedMemoryDependencyException);

        this.memoryBrokerMock.Verify(broker =>
            broker.SelectMemoriesAsync(),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(
                expectedMemoryDependencyException))),
                    Times.Once);

        this.memoryBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyExceptionOnRecallMemoriesIfIOErrorOccursAndLogItAsync()
    {
        // given
        var ioException = new IOException();

        var failedMemoryDependencyException =
            new FailedMemoryDependencyException(
                message: "Failed memory dependency error occurred, contact support.",
                innerException: ioException);

        var expectedMemoryDependencyException =
            new MemoryDependencyException(
                message: "Memory dependency error occurred, contact support.",
                innerException: failedMemoryDependencyException);

        this.memoryBrokerMock.Setup(broker =>
            broker.SelectMemoriesAsync())
                .ThrowsAsync(ioException);

        // when
        ValueTask<IReadOnlyList<string>> recallTask =
            this.memoryService.RecallMemoriesAsync();

        MemoryDependencyException actualMemoryDependencyException =
            await Assert.ThrowsAsync<MemoryDependencyException>(
                recallTask.AsTask);

        // then
        actualMemoryDependencyException.Should()
            .BeEquivalentTo(expectedMemoryDependencyException);

        this.memoryBrokerMock.Verify(broker =>
            broker.SelectMemoriesAsync(),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedMemoryDependencyException))),
                    Times.Once);

        this.memoryBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnRecallMemoriesIfServiceErrorOccursAndLogItAsync()
    {
        // given
        var serviceException = new Exception();

        var failedMemoryServiceException =
            new FailedMemoryServiceException(
                message: "Failed memory service error occurred, contact support.",
                innerException: serviceException);

        var expectedMemoryServiceException =
            new MemoryServiceException(
                message: "Memory service error occurred, contact support.",
                innerException: failedMemoryServiceException);

        this.memoryBrokerMock.Setup(broker =>
            broker.SelectMemoriesAsync())
                .ThrowsAsync(serviceException);

        // when
        ValueTask<IReadOnlyList<string>> recallTask =
            this.memoryService.RecallMemoriesAsync();

        MemoryServiceException actualMemoryServiceException =
            await Assert.ThrowsAsync<MemoryServiceException>(
                recallTask.AsTask);

        // then
        actualMemoryServiceException.Should()
            .BeEquivalentTo(expectedMemoryServiceException);

        this.memoryBrokerMock.Verify(broker =>
            broker.SelectMemoriesAsync(),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedMemoryServiceException))),
                    Times.Once);

        this.memoryBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowDependencyExceptionOnRememberIfDependencyErrorOccursAndLogItAsync()
    {
        // given
        string randomMemory = CreateRandomString();
        var ioException = new IOException();

        var failedMemoryDependencyException =
            new FailedMemoryDependencyException(
                message: "Failed memory dependency error occurred, contact support.",
                innerException: ioException);

        var expectedMemoryDependencyException =
            new MemoryDependencyException(
                message: "Memory dependency error occurred, contact support.",
                innerException: failedMemoryDependencyException);

        this.memoryBrokerMock.Setup(broker =>
            broker.InsertMemoryAsync(randomMemory))
                .ThrowsAsync(ioException);

        // when
        ValueTask rememberTask =
            this.memoryService.RememberAsync(randomMemory);

        MemoryDependencyException actualMemoryDependencyException =
            await Assert.ThrowsAsync<MemoryDependencyException>(
                rememberTask.AsTask);

        // then
        actualMemoryDependencyException.Should()
            .BeEquivalentTo(expectedMemoryDependencyException);

        this.memoryBrokerMock.Verify(broker =>
            broker.InsertMemoryAsync(randomMemory),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedMemoryDependencyException))),
                    Times.Once);

        this.memoryBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
    }
