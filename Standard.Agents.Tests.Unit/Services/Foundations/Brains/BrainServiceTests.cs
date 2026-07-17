// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Linq.Expressions;
using Moq;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Services.Foundations.Brains;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Brains;

public partial class BrainServiceTests
{
    private readonly Mock<IGeneratorBroker> generatorBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly IBrainService brainService;

    public BrainServiceTests()
    {
        this.generatorBrokerMock = new Mock<IGeneratorBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.brainService = new BrainService(
            generatorBroker: this.generatorBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static string CreateRandomString() =>
        new MnemonicString().GetValue();

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);
    }
