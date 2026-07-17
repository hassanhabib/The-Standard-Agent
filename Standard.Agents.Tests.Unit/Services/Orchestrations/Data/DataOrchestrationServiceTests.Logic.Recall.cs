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
