// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Brains.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Decision;

public partial class BrainServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldThrowValidationExceptionOnGenerateIfUserPromptIsInvalidAndLogItAsync(
        string invalidUserPrompt)
    {
        // given
        string randomSystemPrompt = CreateRandomString();

        var invalidBrainException =
            new InvalidBrainException(
                message: "Invalid brain input. Please correct the error and try again.");

        var expectedBrainValidationException =
            new BrainValidationException(
                message: "Brain validation error occurred, fix the error and try again.",
                innerException: invalidBrainException);

        // when
        ValueTask<string> generateTask =
            this.brainService.GenerateAsync(randomSystemPrompt, invalidUserPrompt);

        BrainValidationException actualBrainValidationException =
            await Assert.ThrowsAsync<BrainValidationException>(
                generateTask.AsTask);

        // then
        actualBrainValidationException.Should()
            .BeEquivalentTo(expectedBrainValidationException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedBrainValidationException))),
                    Times.Once);

        // Never spend an inference call on a prompt we already know is empty.
        this.generatorBrokerMock.Verify(broker =>
            broker.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

        this.generatorBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    // The system prompt is NOT validated. An agent with no skills configured has an
    // empty system prompt and is still a legal agent — SPEC.md 8.1 lets Core run
    // with Gate and Judge pass-through, and says nothing about requiring skills.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShouldGenerateOnGenerateEvenIfSystemPromptIsEmptyAsync(
        string emptySystemPrompt)
    {
        // given
        string randomUserPrompt = CreateRandomString();
        string randomReply = CreateRandomString();
        string expectedReply = randomReply;

        this.generatorBrokerMock.Setup(broker =>
            broker.GenerateAsync(emptySystemPrompt, randomUserPrompt))
                .ReturnsAsync(randomReply);

        // when
        string actualReply =
            await this.brainService.GenerateAsync(emptySystemPrompt, randomUserPrompt);

        // then
        actualReply.Should().BeEquivalentTo(expectedReply);

        this.generatorBrokerMock.Verify(broker =>
            broker.GenerateAsync(emptySystemPrompt, randomUserPrompt),
                Times.Once);

        this.generatorBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
