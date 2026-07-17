// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Decision;

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

    // The verdict is returned verbatim, including a refusal. The Gate reports what
    // the guardian said; acting on it is ThinkAsync's job (#33). A foundation that
    // swallowed "refuse" into a bool would throw away the reason.
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

    // Invariant 7.2 — the gate prompt is Data, passed through untouched. If this
    // service ever authored or amended the screening rules, the rules would live in
    // Decision instead of Data and could not be audited or changed without a deploy.
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
