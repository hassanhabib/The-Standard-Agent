// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Moq;
using Standard.Agents.Brokers.Classifiers;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Services.Foundations.Gates;
using System.Linq.Expressions;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Gates;

public partial class GateServiceTests
{
    private readonly Mock<IClassifierBroker> classifierBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly IGateService gateService;

    public GateServiceTests()
    {
        this.classifierBrokerMock = new Mock<IClassifierBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.gateService = new GateService(
            classifierBroker: this.classifierBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static string CreateRandomString() =>
        new MnemonicString().GetValue();

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);
}
