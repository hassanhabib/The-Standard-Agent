// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Linq.Expressions;
using Moq;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Services.Foundations.Returns;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Returns;

public partial class ReturnServiceTests
{
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly IReturnService returnService;

    public ReturnServiceTests()
    {
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.returnService = new ReturnService(
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static string CreateRandomString() =>
        new MnemonicString().GetValue();

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);
    }
