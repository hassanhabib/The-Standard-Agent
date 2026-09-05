// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Brokers.Mcps;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.ExternalTools;

public partial class ExternalToolServiceTests
{
    // Found in the 2026-09-04 principal review (F-15): discovery lived in a model carrying a
    // delegate, so it evaded the tier a dependency is reviewed at, and an outage came back as
    // "no tools". Discovery is a foundation routine like any other: the broker's answer in
    // business language, and its failures localized and categorized rather than swallowed.
    [Fact]
    public async Task ShouldRetrieveToolsAsync()
    {
        // given
        IReadOnlyList<McpTool> randomTools =
        [
            new McpTool(CreateRandomString(), CreateRandomString()),
            new McpTool(CreateRandomString(), CreateRandomString())
        ];

        IReadOnlyList<McpTool> expectedTools = randomTools;

        this.mcpBrokerMock.Setup(broker =>
            broker.ListToolsAsync())
                .ReturnsAsync(randomTools);

        // when
        IReadOnlyList<McpTool> actualTools =
            await this.externalToolService.RetrieveToolsAsync();

        // then
        actualTools.Should().BeEquivalentTo(expectedTools);

        this.mcpBrokerMock.Verify(broker =>
            broker.ListToolsAsync(),
                Times.Once);

        this.mcpBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
