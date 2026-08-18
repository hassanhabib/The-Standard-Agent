// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Linq.Expressions;
using Moq;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Usages;
using Standard.Agents.Services.Foundations.Usages;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Usages;

public partial class UsageServiceTests
{
    private readonly Mock<IUsageBroker> usageBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly IUsageService usageService;

    public UsageServiceTests()
    {
        this.usageBrokerMock = new Mock<IUsageBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.usageService = new UsageService(
            usageBroker: this.usageBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static string CreateRandomString() =>
        new MnemonicString().GetValue();

    private static int CreateRandomTokenCount() =>
        new IntRange(min: 1, max: 4000).GetValue();

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);
}
