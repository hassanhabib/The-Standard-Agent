// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Linq.Expressions;
using Moq;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Services.Foundations.ExternalTools;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Standard.Agents.Tests.Unit.Services.Foundations.ExternalTools;

public partial class ExternalToolServiceTests
{
    private readonly Mock<IMcpBroker> mcpBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly IExternalToolService externalToolService;

    public ExternalToolServiceTests()
    {
        this.mcpBrokerMock = new Mock<IMcpBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.externalToolService = new ExternalToolService(
            mcpBroker: this.mcpBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static string CreateRandomString() =>
        new MnemonicString().GetValue();

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);
}
