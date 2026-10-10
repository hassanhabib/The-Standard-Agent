// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Knowledges;
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Coordinations.DataNature;

public partial class DataCoordinationServiceTests
{
    [Fact]
    public async Task ShouldSetSystemPromptFromSkillServiceOnRecallAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        string randomSkills = CreateRandomString();
        string expectedSystemPrompt = randomSkills;

        this.skillServiceMock.Setup(service =>
            service.RetrieveSkillsAsync())
                .ReturnsAsync(randomSkills);

        this.memoryServiceMock.Setup(service =>
            service.RecallMemoriesAsync())
                .ReturnsAsync([]);

        // when
        AgentContext actualContext =
            await this.dataCoordinationService.RecallAsync(inputContext);

        // then
        actualContext.SystemPrompt.Should().BeEquivalentTo(expectedSystemPrompt);

        this.skillServiceMock.Verify(service =>
            service.RetrieveSkillsAsync(),
                Times.Once);
    }

    [Fact]
    public async Task ShouldNotMutateInputContextOnRecallAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        string originalPrompt = inputContext.Prompt;
        string randomSkills = CreateRandomString();

        this.skillServiceMock.Setup(service =>
            service.RetrieveSkillsAsync())
                .ReturnsAsync(randomSkills);

        this.memoryServiceMock.Setup(service =>
            service.RecallMemoriesAsync())
                .ReturnsAsync([]);

        // when
        AgentContext actualContext =
            await this.dataCoordinationService.RecallAsync(inputContext);

        // then
        inputContext.SystemPrompt.Should().BeEmpty();
        inputContext.Prompt.Should().BeEquivalentTo(originalPrompt);
        actualContext.Should().NotBeSameAs(inputContext);
        actualContext.Prompt.Should().BeEquivalentTo(originalPrompt);
    }

    [Fact]
    public async Task ShouldSeedObservationsFromMemoriesOnRecallAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        string randomSkills = CreateRandomString();
        List<string> randomMemories = [CreateRandomString(), CreateRandomString()];

        this.skillServiceMock.Setup(service =>
            service.RetrieveSkillsAsync())
                .ReturnsAsync(randomSkills);

        this.memoryServiceMock.Setup(service =>
            service.RecallMemoriesAsync())
                .ReturnsAsync(randomMemories);

        // when
        AgentContext actualContext =
            await this.dataCoordinationService.RecallAsync(inputContext);

        // then
        actualContext.Observations.Should().Contain(randomMemories);

        this.memoryServiceMock.Verify(service =>
            service.RecallMemoriesAsync(),
                Times.Once);
    }

    [Fact]
    public async Task ShouldPreserveExistingObservationsOnRecallAsync()
    {
        // given
        string priorObservation = CreateRandomString();

        AgentContext inputContext = new()
        {
            Prompt = CreateRandomString(),
            Observations = [priorObservation]
        };

        this.skillServiceMock.Setup(service =>
            service.RetrieveSkillsAsync())
                .ReturnsAsync(CreateRandomString());

        this.memoryServiceMock.Setup(service =>
            service.RecallMemoriesAsync())
                .ReturnsAsync([]);

        // when
        AgentContext actualContext =
            await this.dataCoordinationService.RecallAsync(inputContext);

        // then
        actualContext.Observations.Should().Contain(priorObservation);
    }

    [Fact]
    public async Task ShouldExpandToolsMarkerInSystemPromptOnRecallAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        string skillsWithMarker = "You may use these tools:\n{{tools}}\nChoose one.";

        this.skillServiceMock.Setup(service =>
            service.RetrieveSkillsAsync())
                .ReturnsAsync(skillsWithMarker);

        this.memoryServiceMock.Setup(service =>
            service.RecallMemoriesAsync())
                .ReturnsAsync([]);

        // when
        AgentContext actualContext =
            await this.dataCoordinationService.RecallAsync(inputContext);

        // then
        actualContext.SystemPrompt.Should().Contain(ToolCatalog);
        actualContext.SystemPrompt.Should().NotContain("{{tools}}");
    }

    [Fact]
    public async Task ShouldSeedKnowledgeIntoObservationsOnRecallAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();

        List<KnowledgeResult> randomKnowledgeResults =
            [CreateRandomKnowledgeResult(CreateRandomString()), CreateRandomKnowledgeResult(string.Empty)];

        List<string> expectedObservations =
            [.. randomKnowledgeResults.Select(knowledgeResult => knowledgeResult.Text)];

        this.skillServiceMock.Setup(service =>
            service.RetrieveSkillsAsync())
                .ReturnsAsync(CreateRandomString());

        this.memoryServiceMock.Setup(service =>
            service.RecallMemoriesAsync())
                .ReturnsAsync([]);

        this.knowledgeServiceMock.Setup(service =>
            service.RetrieveSourcedKnowledgeAsync(It.IsAny<string>()))
                .ReturnsAsync(randomKnowledgeResults);

        // when
        AgentContext actualContext =
            await this.dataCoordinationService.RecallAsync(inputContext);

        // then — the passages alone, exactly as an unsourced broker's would read
        actualContext.Observations.Should().Equal(expectedObservations);

        this.knowledgeServiceMock.Verify(service =>
            service.RetrieveSourcedKnowledgeAsync(inputContext.Prompt),
                Times.Once);

        this.knowledgeServiceMock.VerifyNoOtherCalls();
    }

    // The run's sources, once each and in the order first recalled (SPEC.md §3.2). Recall runs
    // every turn, so a source an earlier turn recalled is already on the context and keeps its
    // place; a passage with no source adds nothing, because it can never be cited.
    [Fact]
    public async Task ShouldCarryTheSourcesOfRecalledKnowledgeOnRecallAsync()
    {
        // given
        string earlierSource = CreateRandomString();
        string newSource = CreateRandomString();

        AgentContext inputContext =
            CreateRandomAgentContext() with { GroundingSources = [earlierSource] };

        List<KnowledgeResult> randomKnowledgeResults =
        [
            CreateRandomKnowledgeResult(newSource),
            CreateRandomKnowledgeResult(string.Empty),
            CreateRandomKnowledgeResult(earlierSource),
            CreateRandomKnowledgeResult(newSource)
        ];

        List<string> expectedGroundingSources = [earlierSource, newSource];

        this.skillServiceMock.Setup(service =>
            service.RetrieveSkillsAsync())
                .ReturnsAsync(CreateRandomString());

        this.memoryServiceMock.Setup(service =>
            service.RecallMemoriesAsync())
                .ReturnsAsync([]);

        this.knowledgeServiceMock.Setup(service =>
            service.RetrieveSourcedKnowledgeAsync(inputContext.Prompt))
                .ReturnsAsync(randomKnowledgeResults);

        // when
        AgentContext actualContext =
            await this.dataCoordinationService.RecallAsync(inputContext);

        // then
        actualContext.GroundingSources.Should().Equal(expectedGroundingSources);

        this.knowledgeServiceMock.Verify(service =>
            service.RetrieveSourcedKnowledgeAsync(inputContext.Prompt),
                Times.Once);

        this.knowledgeServiceMock.VerifyNoOtherCalls();
    }
}
