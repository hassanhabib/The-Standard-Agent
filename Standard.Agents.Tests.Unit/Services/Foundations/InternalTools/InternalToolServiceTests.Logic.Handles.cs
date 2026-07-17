// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.InternalTools;

public partial class InternalToolServiceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldHandlesAsync(bool brokerHasTool)
    {
        // given
        string randomName = CreateRandomString();
        string inputName = randomName;
        bool expectedOutcome = brokerHasTool;

        this.toolBrokerMock.Setup(broker =>
            broker.HasAsync(inputName))
                .ReturnsAsync(brokerHasTool);

        // when
        bool actualOutcome =
            await this.internalToolService.HandlesAsync(inputName);

        // then
        actualOutcome.Should().Be(expectedOutcome);

        this.toolBrokerMock.Verify(broker =>
            broker.HasAsync(inputName),
                Times.Once);

        this.toolBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
    }
