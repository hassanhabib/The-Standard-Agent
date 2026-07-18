// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.InternalTools.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.InternalTools;

public partial class InternalToolServiceTests
{
    [Fact]
    public async Task ShouldThrowServiceExceptionOnHandlesIfServiceErrorOccursAndLogItAsync()
    {
        // given
        string randomName = CreateRandomString();
        string inputName = randomName;
        var serviceException = new Exception();

        var failedInternalToolServiceException =
            new FailedInternalToolServiceException(
                message: "Failed internal tool service error occurred, contact support.",
                innerException: serviceException);

        var expectedInternalToolServiceException =
            new InternalToolServiceException(
                message: "Internal tool service error occurred, contact support.",
                innerException: failedInternalToolServiceException);

        this.toolBrokerMock.Setup(broker =>
            broker.HasAsync(inputName))
                .ThrowsAsync(serviceException);

        // when
        ValueTask<bool> handlesTask =
            this.internalToolService.HandlesAsync(inputName);

        InternalToolServiceException actualInternalToolServiceException =
            await Assert.ThrowsAsync<InternalToolServiceException>(
                handlesTask.AsTask);

        // then
        actualInternalToolServiceException.Should()
            .BeEquivalentTo(expectedInternalToolServiceException);

        this.toolBrokerMock.Verify(broker =>
            broker.HasAsync(inputName),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedInternalToolServiceException))),
                    Times.Once);

        this.toolBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
