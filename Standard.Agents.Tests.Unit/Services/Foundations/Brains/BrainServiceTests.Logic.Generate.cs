// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Brains;

public partial class BrainServiceTests
{
    [Fact]
    public async Task ShouldGenerateAsync()
    {
        // given
        string randomSystemPrompt = CreateRandomString();
        string randomUserPrompt = CreateRandomString();
        string randomReply = CreateRandomString();
        string inputSystemPrompt = randomSystemPrompt;
        string inputUserPrompt = randomUserPrompt;
        string expectedReply = randomReply;

        this.generatorBrokerMock.Setup(broker =>
            broker.GenerateAsync(inputSystemPrompt, inputUserPrompt))
                .ReturnsAsync(randomReply);

        // when
        string actualReply =
            await this.brainService.GenerateAsync(inputSystemPrompt, inputUserPrompt);

        // then
        actualReply.Should().BeEquivalentTo(expectedReply);

        this.generatorBrokerMock.Verify(broker =>
            broker.GenerateAsync(inputSystemPrompt, inputUserPrompt),
                Times.Once);

        this.generatorBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldPassPromptsThroughUnalteredOnGenerateAsync()
    {
        // given
        string systemPromptWithFormatting = "  You are an agent.\n\nRules:\n- be terse  ";
        string userPromptWithFormatting = "  what is 1+1?  ";
        string randomReply = CreateRandomString();
        string expectedReply = randomReply;

        this.generatorBrokerMock.Setup(broker =>
            broker.GenerateAsync(systemPromptWithFormatting, userPromptWithFormatting))
                .ReturnsAsync(randomReply);

        // when
        string actualReply = await this.brainService.GenerateAsync(
            systemPromptWithFormatting,
            userPromptWithFormatting);

        // then
        actualReply.Should().BeEquivalentTo(expectedReply);

        this.generatorBrokerMock.Verify(broker =>
            broker.GenerateAsync(systemPromptWithFormatting, userPromptWithFormatting),
                Times.Once);

        this.generatorBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
    }
