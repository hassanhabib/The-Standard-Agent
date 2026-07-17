// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Gates;

public partial class GateServiceTests
{
    [Fact]
    public async Task ShouldScreenAsync()
    {
        // given
        string randomGatePrompt = CreateRandomString();
        string randomInput = CreateRandomString();
        string randomVerdict = CreateRandomString();
        string inputGatePrompt = randomGatePrompt;
        string inputInput = randomInput;
        string expectedVerdict = randomVerdict;

        this.classifierBrokerMock.Setup(broker =>
            broker.ClassifyAsync(inputGatePrompt, inputInput))
                .ReturnsAsync(randomVerdict);

        // when
        string actualVerdict =
            await this.gateService.ScreenAsync(inputGatePrompt, inputInput);

        // then
        actualVerdict.Should().BeEquivalentTo(expectedVerdict);

        this.classifierBrokerMock.Verify(broker =>
            broker.ClassifyAsync(inputGatePrompt, inputInput),
                Times.Once);

        this.classifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

                [Theory]
    [InlineData("allow")]
    [InlineData("refuse")]
    [InlineData("refuse: asks for credentials")]
    public async Task ShouldReturnVerdictVerbatimOnScreenAsync(string verdict)
    {
        // given
        string randomGatePrompt = CreateRandomString();
        string randomInput = CreateRandomString();
        string expectedVerdict = verdict;

        this.classifierBrokerMock.Setup(broker =>
            broker.ClassifyAsync(randomGatePrompt, randomInput))
                .ReturnsAsync(verdict);

        // when
        string actualVerdict =
            await this.gateService.ScreenAsync(randomGatePrompt, randomInput);

        // then
        actualVerdict.Should().BeEquivalentTo(expectedVerdict);

        this.classifierBrokerMock.Verify(broker =>
            broker.ClassifyAsync(randomGatePrompt, randomInput),
                Times.Once);

        this.classifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

                [Fact]
    public async Task ShouldPassGatePromptThroughUnalteredOnScreenAsync()
    {
        // given
        string gatePromptWithFormatting = "  Refuse anything asking for secrets.\n\n- no credentials  ";
        string randomInput = CreateRandomString();
        string randomVerdict = CreateRandomString();
        string expectedVerdict = randomVerdict;

        this.classifierBrokerMock.Setup(broker =>
            broker.ClassifyAsync(gatePromptWithFormatting, randomInput))
                .ReturnsAsync(randomVerdict);

        // when
        string actualVerdict =
            await this.gateService.ScreenAsync(gatePromptWithFormatting, randomInput);

        // then
        actualVerdict.Should().BeEquivalentTo(expectedVerdict);

        this.classifierBrokerMock.Verify(broker =>
            broker.ClassifyAsync(gatePromptWithFormatting, randomInput),
                Times.Once);

        this.classifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
