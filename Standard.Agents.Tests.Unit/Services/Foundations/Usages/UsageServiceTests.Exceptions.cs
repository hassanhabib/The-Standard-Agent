// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Usages;
using Standard.Agents.Models.Foundations.Usages.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Usages;

public partial class UsageServiceTests
{
    // A counter that cannot be reached is the COUNTER's failure, not the measurement's. The
    // caller can carry on with a provider-reported number when it has one — but only if the
    // failure names the dependency rather than being flattened into a service error.
    [Fact]
    public async Task ShouldThrowDependencyExceptionOnMeasureIfCounterIsUnreachableAsync()
    {
        // given
        var httpRequestException = new HttpRequestException();

        var failedUsageDependencyException =
            new FailedUsageDependencyException(
                message: "Failed usage dependency error occurred, contact support.",
                innerException: httpRequestException);

        var expectedUsageDependencyException =
            new UsageDependencyException(
                message: "Usage dependency error occurred, contact support.",
                innerException: failedUsageDependencyException);

        this.usageBrokerMock.Setup(broker =>
            broker.CountTokensAsync(It.IsAny<string>()))
                .ThrowsAsync(httpRequestException);

        // when
        ValueTask<AgentUsage> measureTask =
            this.usageService.MeasureAsync(CreateRandomString(), CreateRandomString());

        UsageDependencyException actualException =
            await Assert.ThrowsAsync<UsageDependencyException>(measureTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedUsageDependencyException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedUsageDependencyException))),
                Times.Once);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnMeasureIfCounterFailsAsync()
    {
        // given
        var serviceException = new Exception();

        var failedUsageServiceException =
            new FailedUsageServiceException(
                message: "Failed usage service error occurred, contact support.",
                innerException: serviceException);

        var expectedUsageServiceException =
            new UsageServiceException(
                message: "Usage service error occurred, contact support.",
                innerException: failedUsageServiceException);

        this.usageBrokerMock.Setup(broker =>
            broker.CountTokensAsync(It.IsAny<string>()))
                .ThrowsAsync(serviceException);

        // when
        ValueTask<AgentUsage> measureTask =
            this.usageService.MeasureAsync(CreateRandomString(), CreateRandomString());

        UsageServiceException actualException =
            await Assert.ThrowsAsync<UsageServiceException>(measureTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedUsageServiceException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(expectedUsageServiceException))),
                Times.Once);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
