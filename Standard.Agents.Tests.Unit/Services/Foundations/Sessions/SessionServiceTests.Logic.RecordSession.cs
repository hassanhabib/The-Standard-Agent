// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Moq;
using Standard.Agents.Models.Brokers.Sessions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Sessions;

public partial class SessionServiceTests
{
    [Fact]
    public async Task ShouldRecordSessionAsync()
    {
        // given
        AgentSession randomSession = CreateRandomSession();
        AgentSession inputSession = randomSession;

        // when
        await this.sessionService.RecordSessionAsync(inputSession);

        // then
        this.sessionBrokerMock.Verify(broker =>
            broker.UpsertSessionAsync(inputSession),
                Times.Once);

        this.sessionBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
