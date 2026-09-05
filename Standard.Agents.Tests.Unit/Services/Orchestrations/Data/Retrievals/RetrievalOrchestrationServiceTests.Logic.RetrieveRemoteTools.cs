// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Brokers.Mcps;
using Xeptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Data.Retrievals;

public partial class RetrievalOrchestrationServiceTests
{
    // Found in the 2026-09-04 principal review (F-04): the remote catalog was appended after
    // selection ran, so a selector could neither see nor withhold a remote tool. Discovery is a
    // retrieval the loop can ask for BEFORE it decides what a run is offered — every tool the
    // servers declared, described or not, so the callers apply the opt-in themselves.
    [Fact]
    public async Task ShouldRetrieveRemoteToolsAsync()
    {
        // given
        IReadOnlyList<McpTool> randomTools =
        [
            new McpTool(CreateRandomString(), CreateRandomString()),
            new McpTool(CreateRandomString(), "")
        ];

        IReadOnlyList<McpTool> expectedTools = randomTools;

        this.externalToolServiceMock.Setup(service =>
            service.RetrieveToolsAsync())
                .ReturnsAsync(randomTools);

        // when
        IReadOnlyList<McpTool> actualTools =
            await this.retrievalOrchestrationService.RetrieveRemoteToolsAsync();

        // then
        actualTools.Should().BeEquivalentTo(expectedTools);

        this.externalToolServiceMock.Verify(service =>
            service.RetrieveToolsAsync(),
                Times.Once);

        this.skillServiceMock.VerifyNoOtherCalls();
        this.externalToolServiceMock.VerifyNoOtherCalls();
        this.knowledgeServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    // The same degradation the catalog gets: a server that cannot be asked offers nothing this
    // turn, and the foundation already logged why.
    [Theory]
    [MemberData(nameof(RemoteDiscoveryExceptions))]
    public async Task ShouldRetrieveNoRemoteToolsWhenDiscoveryFailsAsync(
        Xeption remoteDiscoveryException)
    {
        // given
        this.externalToolServiceMock.Setup(service =>
            service.RetrieveToolsAsync())
                .ThrowsAsync(remoteDiscoveryException);

        // when
        IReadOnlyList<McpTool> actualTools =
            await this.retrievalOrchestrationService.RetrieveRemoteToolsAsync();

        // then
        actualTools.Should().BeEmpty();

        this.externalToolServiceMock.Verify(service =>
            service.RetrieveToolsAsync(),
                Times.Once);

        this.skillServiceMock.VerifyNoOtherCalls();
        this.externalToolServiceMock.VerifyNoOtherCalls();
        this.knowledgeServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
