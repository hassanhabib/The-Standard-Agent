// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.ExternalTools;

public partial class ExternalToolServiceTests
{
    [Fact]
    public async Task ShouldCallAsync()
    {
        // given
        string randomName = CreateRandomString();
        string randomInput = CreateRandomString();
        string randomOutput = CreateRandomString();
        string inputName = randomName;
        string inputInput = randomInput;
        string expectedOutput = randomOutput;

        this.mcpBrokerMock.Setup(broker =>
            broker.CallAsync(inputName, inputInput))
                .ReturnsAsync(randomOutput);

        // when
        string actualOutput =
            await this.externalToolService.CallAsync(inputName, inputInput);

        // then
        actualOutput.Should().BeEquivalentTo(expectedOutput);

        this.mcpBrokerMock.Verify(broker =>
            broker.CallAsync(inputName, inputInput),
                Times.Once);

        this.mcpBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldReturnToolOutputOnCallEvenIfToolReportsAnErrorAsync()
    {
        // given
        string randomName = CreateRandomString();
        string randomInput = CreateRandomString();
        string toolReportedError = "error: upstream returned no data";
        string expectedOutput = toolReportedError;

        this.mcpBrokerMock.Setup(broker =>
            broker.CallAsync(randomName, randomInput))
                .ReturnsAsync(toolReportedError);

        // when
        string actualOutput =
            await this.externalToolService.CallAsync(randomName, randomInput);

        // then
        actualOutput.Should().BeEquivalentTo(expectedOutput);

        this.mcpBrokerMock.Verify(broker =>
            broker.CallAsync(randomName, randomInput),
                Times.Once);

        this.mcpBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
