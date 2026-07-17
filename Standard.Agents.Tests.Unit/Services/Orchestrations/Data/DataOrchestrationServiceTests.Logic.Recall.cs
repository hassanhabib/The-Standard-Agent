// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Data;

public partial class DataOrchestrationServiceTests
{
    // SPEC.md 4.3: recall MUST set systemPrompt from SkillService.
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
            await this.dataOrchestrationService.RecallAsync(inputContext);

        // then
        actualContext.SystemPrompt.Should().BeEquivalentTo(expectedSystemPrompt);

        this.skillServiceMock.Verify(service =>
            service.RetrieveSkillsAsync(),
                Times.Once);
    }

    // Copy-on-write: SPEC.md 3 makes it a MUST. The input instance must come back
    // untouched, and the returned one must be a different object.
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
            await this.dataOrchestrationService.RecallAsync(inputContext);

        // then
        inputContext.SystemPrompt.Should().BeEmpty();
        inputContext.Prompt.Should().BeEquivalentTo(originalPrompt);
        actualContext.Should().NotBeSameAs(inputContext);
        actualContext.Prompt.Should().BeEquivalentTo(originalPrompt);
    }

    // recall MAY seed observations from Memory. Memories arrive as observations the
    // Brain can read on the next Think.
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
            await this.dataOrchestrationService.RecallAsync(inputContext);

        // then
        actualContext.Observations.Should().Contain(randomMemories);

        this.memoryServiceMock.Verify(service =>
            service.RecallMemoriesAsync(),
                Times.Once);
    }

    // Observations accumulate across turns. Recall runs EVERY turn (SPEC.md 5), so
    // if it replaced observations instead of preserving them, every tool result
    // Direction fed back would be wiped before the Brain ever saw it — and vector
    // 02 (tool-then-final) could never pass.
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
            await this.dataOrchestrationService.RecallAsync(inputContext);

        // then
        actualContext.Observations.Should().Contain(priorObservation);
    }
}
