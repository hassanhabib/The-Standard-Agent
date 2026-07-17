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
        string randomInput = CreateRandomString();
        string randomVerdict = CreateRandomString();
        string inputInput = randomInput;
        string expectedVerdict = randomVerdict;

        this.classifierBrokerMock.Setup(broker =>
            broker.ClassifyAsync(inputInput))
                .ReturnsAsync(randomVerdict);

        // when
        string actualVerdict =
            await this.gateService.ScreenAsync(inputInput);

        // then
        actualVerdict.Should().BeEquivalentTo(expectedVerdict);

        this.classifierBrokerMock.Verify(broker =>
            broker.ClassifyAsync(inputInput),
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
        string randomInput = CreateRandomString();
        string expectedVerdict = verdict;

        this.classifierBrokerMock.Setup(broker =>
            broker.ClassifyAsync(randomInput))
                .ReturnsAsync(verdict);

        // when
        string actualVerdict =
            await this.gateService.ScreenAsync(randomInput);

        // then
        actualVerdict.Should().BeEquivalentTo(expectedVerdict);

        this.classifierBrokerMock.Verify(broker =>
            broker.ClassifyAsync(randomInput),
                Times.Once);

        this.classifierBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
    }
