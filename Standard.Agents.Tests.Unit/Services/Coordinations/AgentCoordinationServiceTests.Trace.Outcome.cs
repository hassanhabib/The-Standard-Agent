// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Moq;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Coordinations;

public partial class AgentCoordinationServiceTests
{
    [Fact]
    public async Task ShouldNarrateRunOutcomeOnProcessPromptAsync()
    {
        // given
        string randomPrompt = CreateRandomString();
        SetupOrchestrationsPassThrough();
        SetupDirectionTerminates(CreateRandomString());

        // when
        await this.agentCoordinationService.ProcessPromptAsync(randomPrompt);

        // then
        this.loggingBrokerMock.Verify(broker =>
            broker.LogOutcomeAsync(It.Is<string>(message => message.Contains("done"))),
                Times.Once);
    }
}
