// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Linq.Expressions;
using Moq;
using Standard.Agents.Brokers.Files;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Foundations.Knowledges;
using Standard.Agents.Services.Foundations.Knowledges;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Knowledges;

public partial class KnowledgeServiceTests
{
    private readonly Mock<IKnowledgeBroker> knowledgeBrokerMock;
    private readonly Mock<ISourcedKnowledgeBroker> sourcedKnowledgeBrokerMock;
    private readonly Mock<IFileBroker> fileBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;
    private readonly IKnowledgeService knowledgeService;
    private readonly IKnowledgeService sourcedKnowledgeService;

    public KnowledgeServiceTests()
    {
        this.knowledgeBrokerMock = new Mock<IKnowledgeBroker>();
        this.sourcedKnowledgeBrokerMock = new Mock<ISourcedKnowledgeBroker>();
        this.fileBrokerMock = new Mock<IFileBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.knowledgeService = new KnowledgeService(
            knowledgeBroker: this.knowledgeBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);

        this.sourcedKnowledgeService = new KnowledgeService(
            sourcedKnowledgeBroker: this.sourcedKnowledgeBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static string CreateRandomString() =>
        new MnemonicString().GetValue();

    private static List<string> CreateRandomDocuments() =>
        Enumerable.Range(0, 3).Select(_ => CreateRandomString()).ToList();

    private static double GetRandomScore() =>
        new DoubleRange(minValue: 0.1, maxValue: 10.0).GetValue();

    private static List<KnowledgeResult> CreateRandomKnowledgeResults()
    {
        return Enumerable.Range(0, 3)
            .Select(_ => new KnowledgeResult
            {
                Text = CreateRandomString(),
                Score = GetRandomScore(),
                Source = CreateRandomString()
            })
                .ToList();
    }

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);
}
