// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Linq.Expressions;
using Moq;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Sessions;
using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Services.Foundations.Sessions;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Sessions;

public partial class SessionServiceTests
{
    private readonly Mock<ISessionBroker> sessionBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly ISessionService sessionService;

    public SessionServiceTests()
    {
        this.sessionBrokerMock = new Mock<ISessionBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.sessionService = new SessionService(
            sessionBroker: this.sessionBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static string CreateRandomString() =>
        new MnemonicString().GetValue();

    private static AgentSession CreateRandomSession() =>
        new()
        {
            Id = CreateRandomString(),
            History = [new AgentTurn(CreateRandomString(), CreateRandomString())],
            RunId = CreateRandomString()
        };

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);
}
