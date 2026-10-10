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
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnRetrieveSourcedKnowledgeIfQueryIsInvalidAndLogItAsync(
string? invalidQuery)
    {
        // given
        var invalidKnowledgeException =
            new InvalidKnowledgeException(
                message: "Invalid knowledge query. Please correct the error and try again.");

        var expectedKnowledgeValidationException =
            new KnowledgeValidationException(
                message: "Knowledge validation error occurred, fix the error and try again.",
                innerException: invalidKnowledgeException);

        // when
        ValueTask<IReadOnlyList<KnowledgeResult>> retrieveTask =
            this.sourcedKnowledgeService.RetrieveSourcedKnowledgeAsync(invalidQuery!);

        KnowledgeValidationException actualKnowledgeValidationException =
            await Assert.ThrowsAsync<KnowledgeValidationException>(
                retrieveTask.AsTask);

        // then
        actualKnowledgeValidationException.Should()
            .BeEquivalentTo(expectedKnowledgeValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedKnowledgeValidationException))),
                    Times.Once);

        this.sourcedKnowledgeBrokerMock.Verify(broker =>
            broker.SelectSourcedKnowledgeAsync(It.IsAny<string>()),
                Times.Never);

        this.sourcedKnowledgeBrokerMock.VerifyNoOtherCalls();
        this.knowledgeBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
