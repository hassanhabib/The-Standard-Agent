// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Linq.Expressions;
using Moq;
using Standard.Agents.Brokers.Data;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Services.Foundations.Data;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Data;

public partial class SkillServiceTests
{
    private readonly Mock<ISkillBroker> skillBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly ISkillService skillService;

    public SkillServiceTests()
    {
        this.skillBrokerMock = new Mock<ISkillBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.skillService = new SkillService(
            skillBroker: this.skillBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static string CreateRandomString() =>
        new MnemonicString().GetValue();

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);
}
