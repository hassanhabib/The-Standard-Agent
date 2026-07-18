// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Threading;
using FluentAssertions;
using Moq;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Brains;

public partial class BrainServiceTests
{
    [Fact]
    public async Task ShouldGenerateStreamAsync()
    {
        // given
        string randomSystemPrompt = CreateRandomString();
        string randomUserPrompt = CreateRandomString();
        string[] randomTokens = ["Hello", ", ", "world"];
        string[] expectedTokens = randomTokens;

        this.generatorBrokerMock.Setup(broker =>
            broker.GenerateStreamAsync(
                randomSystemPrompt,
                randomUserPrompt,
                It.IsAny<CancellationToken>()))
                    .Returns(ToAsyncStream(randomTokens));

        // when
        List<string> actualTokens =
            await DrainAsync(
                this.brainService.GenerateStreamAsync(randomSystemPrompt, randomUserPrompt));

        // then
        actualTokens.Should().Equal(expectedTokens);

        this.generatorBrokerMock.Verify(broker =>
            broker.GenerateStreamAsync(
                randomSystemPrompt,
                randomUserPrompt,
                It.IsAny<CancellationToken>()),
                    Times.Once);

        this.generatorBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
    }
