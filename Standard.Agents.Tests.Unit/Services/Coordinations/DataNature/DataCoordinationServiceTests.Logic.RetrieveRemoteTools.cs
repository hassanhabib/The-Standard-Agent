// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Brokers.Mcps;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Coordinations.DataNature;

public partial class DataCoordinationServiceTests
{
    // What the agent HAS includes what its servers offer: the nature hands the loop the remote
    // tools its retrieval region discovered, so selection can judge them with the local ones
    // (SPEC.md §4.15; principal review 2026-09-04, F-04).
    [Fact]
    public async Task ShouldRetrieveRemoteToolsAsync()
    {
        // given
        IReadOnlyList<McpTool> randomTools =
        [
            new McpTool(CreateRandomString(), CreateRandomString())
        ];

        IReadOnlyList<McpTool> expectedTools = randomTools;

        this.externalToolServiceMock.Setup(service =>
            service.RetrieveToolsAsync())
                .ReturnsAsync(randomTools);

        // when
        IReadOnlyList<McpTool> actualTools =
            await this.dataCoordinationService.RetrieveRemoteToolsAsync();

        // then
        actualTools.Should().BeEquivalentTo(expectedTools);

        this.externalToolServiceMock.Verify(service =>
            service.RetrieveToolsAsync(),
                Times.Once);

        this.externalToolServiceMock.VerifyNoOtherCalls();
        this.skillServiceMock.VerifyNoOtherCalls();
        this.memoryServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
