// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Direction;

public partial class InternalToolServiceTests
{
    [Fact]
    public async Task ShouldRunAsync()
    {
        // given
        string randomName = CreateRandomString();
        string randomInput = CreateRandomString();
        string randomOutput = CreateRandomString();
        string inputName = randomName;
        string inputInput = randomInput;
        string expectedOutput = randomOutput;

        this.toolBrokerMock.Setup(broker =>
            broker.RunAsync(inputName, inputInput))
                .ReturnsAsync(randomOutput);

        // when
        string actualOutput =
            await this.internalToolService.RunAsync(inputName, inputInput);

        // then
        actualOutput.Should().BeEquivalentTo(expectedOutput);

        this.toolBrokerMock.Verify(broker =>
            broker.RunAsync(inputName, inputInput),
                Times.Once);

        this.toolBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

                [Fact]
    public async Task ShouldReturnToolOutputOnRunEvenIfToolReportsAnErrorAsync()
    {
        // given
        string randomName = CreateRandomString();
        string randomInput = CreateRandomString();
        string toolReportedError = "error: could not parse expression";
        string inputName = randomName;
        string inputInput = randomInput;
        string expectedOutput = toolReportedError;

        this.toolBrokerMock.Setup(broker =>
            broker.RunAsync(inputName, inputInput))
                .ReturnsAsync(toolReportedError);

        // when
        string actualOutput =
            await this.internalToolService.RunAsync(inputName, inputInput);

        // then
        actualOutput.Should().BeEquivalentTo(expectedOutput);

        this.toolBrokerMock.Verify(broker =>
            broker.RunAsync(inputName, inputInput),
                Times.Once);

        this.toolBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
