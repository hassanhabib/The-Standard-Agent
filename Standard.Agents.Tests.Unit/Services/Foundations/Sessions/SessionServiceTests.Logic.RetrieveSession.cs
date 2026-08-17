// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Brokers.Sessions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Sessions;

public partial class SessionServiceTests
{
    [Fact]
    public async Task ShouldRetrieveSessionAsync()
    {
        // given
        string randomSessionId = CreateRandomString();
        AgentSession randomSession = CreateRandomSession();
        AgentSession retrievedSession = randomSession;
        AgentSession expectedSession = retrievedSession;

        this.sessionBrokerMock.Setup(broker =>
            broker.SelectSessionAsync(randomSessionId))
                .ReturnsAsync(retrievedSession);

        // when
        AgentSession? actualSession =
            await this.sessionService.RetrieveSessionAsync(randomSessionId);

        // then
        actualSession.Should().BeEquivalentTo(expectedSession);

        this.sessionBrokerMock.Verify(broker =>
            broker.SelectSessionAsync(randomSessionId),
                Times.Once);

        this.sessionBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    // A conversation that has not started yet is not an error. It is the ordinary first prompt,
    // and the caller has to be able to tell it apart from a failure to read.
    [Fact]
    public async Task ShouldRetrieveNoSessionOnRetrieveSessionIfNoneExistsAsync()
    {
        // given
        string randomSessionId = CreateRandomString();

        this.sessionBrokerMock.Setup(broker =>
            broker.SelectSessionAsync(randomSessionId))
                .ReturnsAsync((AgentSession?)null);

        // when
        AgentSession? actualSession =
            await this.sessionService.RetrieveSessionAsync(randomSessionId);

        // then
        actualSession.Should().BeNull();

        this.sessionBrokerMock.Verify(broker =>
            broker.SelectSessionAsync(randomSessionId),
                Times.Once);

        this.sessionBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
