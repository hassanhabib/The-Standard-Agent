// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Linq.Expressions;
using Moq;
using Standard.Agents.Brokers.Decision;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Services.Foundations.Decision;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Decision;

public partial class JudgeServiceTests
{
    private readonly Mock<IVerifierBroker> verifierBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly IJudgeService judgeService;

    public JudgeServiceTests()
    {
        this.verifierBrokerMock = new Mock<IVerifierBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.judgeService = new JudgeService(
            verifierBroker: this.verifierBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static string CreateRandomString() =>
        new MnemonicString().GetValue();

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);
}
