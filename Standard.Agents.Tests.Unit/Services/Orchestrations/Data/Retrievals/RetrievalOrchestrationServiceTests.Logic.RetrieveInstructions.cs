// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Brokers.Mcps;
using Standard.Agents.Models.Foundations.ExternalTools.Exceptions;
using Xeptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Data.Retrievals;

public partial class RetrievalOrchestrationServiceTests
{
    // Remote tools join the catalog under the same opt-in as local ones (SPEC.md §6.1): a
    // description is what advertises a tool, so an undescribed remote tool stays callable but
    // unlisted, exactly as an undescribed local one does.
    [Fact]
    public async Task ShouldRetrieveInstructionsWithRemoteToolsWhenTheMarkerIsPresentAsync()
    {
        // given
        string randomRoute = CreateRandomString();
        string inputRoute = randomRoute;
        string skillsWithMarker = "Tools you may use:\n{{tools}}";

        string weatherSchema = """{"type":"object","properties":{"city":{"type":"string"}}}""";

        IReadOnlyList<McpTool> remoteTools =
        [
            new McpTool("weather", "answers weather questions", weatherSchema),
            new McpTool("undocumented", "")
        ];

        // What a tool takes is part of what advertises it (SPEC.md §6.1): the schema the server
        // declared reaches the catalog line, not an empty object (principal review 2026-09-04,
        // F-03).
        string expectedInstructions =
            "Tools you may use:\n"
                + LocalToolCatalog + "\n"
                + "- weather — answers weather questions parameters: " + weatherSchema;

        this.skillServiceMock.Setup(service =>
            service.RetrieveSkillsAsync(inputRoute))
                .ReturnsAsync(skillsWithMarker);

        this.externalToolServiceMock.Setup(service =>
            service.RetrieveToolsAsync())
                .ReturnsAsync(remoteTools);

        // when
        string actualInstructions =
            await this.retrievalOrchestrationService.RetrieveInstructionsAsync(inputRoute);

        // then
        actualInstructions.Should().Be(expectedInstructions);

        this.skillServiceMock.Verify(service =>
            service.RetrieveSkillsAsync(inputRoute),
                Times.Once);

        this.externalToolServiceMock.Verify(service =>
            service.RetrieveToolsAsync(),
                Times.Once);

        this.skillServiceMock.VerifyNoOtherCalls();
        this.externalToolServiceMock.VerifyNoOtherCalls();
        this.knowledgeServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    public static TheoryData<Xeption> RemoteDiscoveryExceptions()
    {
        var innerException =
            new InvalidExternalToolException(message: "Invalid external tool.");

        return new()
        {
            new ExternalToolDependencyException(
                message: "External tool dependency error occurred, contact support.",
                innerException: innerException),

            new ExternalToolDependencyValidationException(
                message: "External tool dependency validation error occurred.",
                innerException: innerException),

            new ExternalToolServiceException(
                message: "External tool service error occurred, contact support.",
                innerException: innerException)
        };
    }

    // A server down at discovery hides only its own tools this turn and never fails the run:
    // the foundation has already localized and logged the failure at the severity it earned, so
    // the orchestration degrades to the local catalog rather than reporting the outage as an
    // empty server or as a failed prompt (principal review 2026-09-04, F-15).
    [Theory]
    [MemberData(nameof(RemoteDiscoveryExceptions))]
    public async Task ShouldRetrieveInstructionsWithLocalToolsOnlyWhenRemoteDiscoveryFailsAsync(
        Xeption remoteDiscoveryException)
    {
        // given
        string randomRoute = CreateRandomString();
        string inputRoute = randomRoute;
        string skillsWithMarker = "Tools you may use:\n{{tools}}";
        string expectedInstructions = "Tools you may use:\n" + LocalToolCatalog;

        this.skillServiceMock.Setup(service =>
            service.RetrieveSkillsAsync(inputRoute))
                .ReturnsAsync(skillsWithMarker);

        this.externalToolServiceMock.Setup(service =>
            service.RetrieveToolsAsync())
                .ThrowsAsync(remoteDiscoveryException);

        // when
        string actualInstructions =
            await this.retrievalOrchestrationService.RetrieveInstructionsAsync(inputRoute);

        // then
        actualInstructions.Should().Be(expectedInstructions);

        this.skillServiceMock.Verify(service =>
            service.RetrieveSkillsAsync(inputRoute),
                Times.Once);

        this.externalToolServiceMock.Verify(service =>
            service.RetrieveToolsAsync(),
                Times.Once);

        this.skillServiceMock.VerifyNoOtherCalls();
        this.externalToolServiceMock.VerifyNoOtherCalls();
        this.knowledgeServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    // An agent that never advertises never pays a discovery call: the marker is the opt-in.
    [Fact]
    public async Task ShouldNotRetrieveRemoteToolsOnRetrieveInstructionsWhenTheMarkerIsAbsentAsync()
    {
        // given
        string randomRoute = CreateRandomString();
        string inputRoute = randomRoute;
        string skillsWithoutMarker = "You are a calculator agent.";
        string expectedInstructions = skillsWithoutMarker;

        this.skillServiceMock.Setup(service =>
            service.RetrieveSkillsAsync(inputRoute))
                .ReturnsAsync(skillsWithoutMarker);

        // when
        string actualInstructions =
            await this.retrievalOrchestrationService.RetrieveInstructionsAsync(inputRoute);

        // then
        actualInstructions.Should().Be(expectedInstructions);

        this.skillServiceMock.Verify(service =>
            service.RetrieveSkillsAsync(inputRoute),
                Times.Once);

        this.externalToolServiceMock.Verify(service =>
            service.RetrieveToolsAsync(),
                Times.Never);

        this.skillServiceMock.VerifyNoOtherCalls();
        this.externalToolServiceMock.VerifyNoOtherCalls();
        this.knowledgeServiceMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
