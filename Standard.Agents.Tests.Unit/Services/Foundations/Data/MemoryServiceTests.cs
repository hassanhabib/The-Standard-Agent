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

public partial class MemoryServiceTests
{
    private readonly Mock<IMemoryBroker> memoryBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly IMemoryService memoryService;

    public MemoryServiceTests()
    {
        this.memoryBrokerMock = new Mock<IMemoryBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.memoryService = new MemoryService(
            memoryBroker: this.memoryBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static string CreateRandomString() =>
        new MnemonicString().GetValue();

    private static List<string> CreateRandomMemories() =>
        Enumerable.Range(0, 3).Select(_ => CreateRandomString()).ToList();

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);
}
