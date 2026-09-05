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
            broker.CallAsync(inputName, ExpectedArgumentsJson(inputInput)))
                .ReturnsAsync(randomOutput);

        // when
        string actualOutput =
            await this.externalToolService.CallAsync(inputName, inputInput);

        // then
        actualOutput.Should().BeEquivalentTo(expectedOutput);

        this.mcpBrokerMock.Verify(broker =>
            broker.CallAsync(inputName, ExpectedArgumentsJson(inputInput)),
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
            broker.CallAsync(randomName, ExpectedArgumentsJson(randomInput)))
                .ReturnsAsync(toolReportedError);

        // when
        string actualOutput =
            await this.externalToolService.CallAsync(randomName, randomInput);

        // then
        actualOutput.Should().BeEquivalentTo(expectedOutput);

        this.mcpBrokerMock.Verify(broker =>
            broker.CallAsync(randomName, ExpectedArgumentsJson(randomInput)),
                Times.Once);

        this.mcpBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    // Found in the 2026-09-04 principal review (F-03): every call was forced into one string
    // argument named "input", so a tool with a typed, multi-property schema could never be
    // called correctly. Plain text still travels as that one argument — it is what the text
    // protocol produces and what a schema-less tool understands — but a JSON object, which is
    // what a native call produces, is handed to the server exactly as the model wrote it.
    [Fact]
    public async Task ShouldCallWithTheArgumentsAsGivenOnCallIfInputIsAJsonObjectAsync()
    {
        // given
        string randomName = CreateRandomString();
        string randomOutput = CreateRandomString();
        string inputName = randomName;
        string structuredInput = """{"city":"Seattle","days":3}""";
        string expectedOutput = randomOutput;

        this.mcpBrokerMock.Setup(broker =>
            broker.CallAsync(inputName, structuredInput))
                .ReturnsAsync(randomOutput);

        // when
        string actualOutput =
            await this.externalToolService.CallAsync(inputName, structuredInput);

        // then
        actualOutput.Should().BeEquivalentTo(expectedOutput);

        this.mcpBrokerMock.Verify(broker =>
            broker.CallAsync(inputName, structuredInput),
                Times.Once);

        this.mcpBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
