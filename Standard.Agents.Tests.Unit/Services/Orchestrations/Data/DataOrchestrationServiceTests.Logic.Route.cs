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
    public async Task ShouldFanInRoutedSkillsUsingContextRouteOnRecallAsync()
    {
        // given
        string routeLabel = CreateRandomString();
        AgentContext inputContext = CreateRandomAgentContext() with { Route = routeLabel };

        this.skillServiceMock.Setup(service =>
            service.RetrieveSkillsAsync(It.IsAny<string>()))
                .ReturnsAsync(CreateRandomString());

        this.memoryServiceMock.Setup(service =>
            service.RecallMemoriesAsync())
                .ReturnsAsync([]);

        // when
        await this.dataOrchestrationService.RecallAsync(inputContext);

        // then — Data fans skills in on the Gate's route label (carried by the loop)
        this.skillServiceMock.Verify(service =>
            service.RetrieveSkillsAsync(routeLabel),
                Times.Once);
    }
}
