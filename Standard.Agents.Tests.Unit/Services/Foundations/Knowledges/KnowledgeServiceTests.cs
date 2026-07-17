// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Services.Foundations.Knowledges;
using System.Linq.Expressions;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Knowledges;

public partial class KnowledgeServiceTests
{
    private readonly Mock<IKnowledgeBroker> knowledgeBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly IKnowledgeService knowledgeService;

    public KnowledgeServiceTests()
    {
        this.knowledgeBrokerMock = new Mock<IKnowledgeBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.knowledgeService = new KnowledgeService(
            knowledgeBroker: this.knowledgeBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static string CreateRandomString() =>
        new MnemonicString().GetValue();

    private static List<string> CreateRandomDocuments() =>
        Enumerable.Range(0, 3).Select(_ => CreateRandomString()).ToList();

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);
}
