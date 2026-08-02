// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Moq;
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Data;

public partial class DataOrchestrationServiceTests
{
    [Fact]
    public async Task ShouldNarrateSystemPromptSentToDecisionOnRecallAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();

        this.skillServiceMock.Setup(service =>
            service.RetrieveSkillsAsync())
                .ReturnsAsync("SKILL-MARKER-XYZ");

        this.memoryServiceMock.Setup(service =>
            service.RecallMemoriesAsync())
                .ReturnsAsync([]);

        // when
        await this.dataOrchestrationService.RecallAsync(inputContext);

        // then
        this.loggingBrokerMock.Verify(broker =>
            broker.LogProcessAsync(
                "Data",
                It.Is<string>(message =>
                    message.Contains("System prompt") && message.Contains("SKILL-MARKER-XYZ")),
                It.IsAny<bool>()),
                    Times.Once);
    }
}
