// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Data;

public partial class KnowledgeServiceTests
{
    [Fact]
    public async Task ShouldRetrieveKnowledgeAsync()
    {
        // given
        string randomQuery = CreateRandomString();
        List<string> randomDocuments = CreateRandomDocuments();
        string inputQuery = randomQuery;
        IReadOnlyList<string> retrievedDocuments = randomDocuments;
        IReadOnlyList<string> expectedDocuments = retrievedDocuments;

        this.knowledgeBrokerMock.Setup(broker =>
            broker.SelectKnowledgeAsync(inputQuery))
                .ReturnsAsync(retrievedDocuments);

        // when
        IReadOnlyList<string> actualDocuments =
            await this.knowledgeService.RetrieveKnowledgeAsync(inputQuery);

        // then
        actualDocuments.Should().BeEquivalentTo(expectedDocuments);

        this.knowledgeBrokerMock.Verify(broker =>
            broker.SelectKnowledgeAsync(inputQuery),
                Times.Once);

        this.knowledgeBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    // Finding nothing is an answer, not a failure. A query with no hits means the
    // agent looked and there was nothing there — which is exactly what Recall should
    // report, rather than throwing and killing a turn that could proceed without it.
    [Fact]
    public async Task ShouldRetrieveNoKnowledgeOnRetrieveKnowledgeIfNothingMatchesAsync()
    {
        // given
        string randomQuery = CreateRandomString();
        IReadOnlyList<string> noDocuments = [];
        IReadOnlyList<string> expectedDocuments = noDocuments;

        this.knowledgeBrokerMock.Setup(broker =>
            broker.SelectKnowledgeAsync(randomQuery))
                .ReturnsAsync(noDocuments);

        // when
        IReadOnlyList<string> actualDocuments =
            await this.knowledgeService.RetrieveKnowledgeAsync(randomQuery);

        // then
        actualDocuments.Should().BeEmpty();
        actualDocuments.Should().BeEquivalentTo(expectedDocuments);

        this.knowledgeBrokerMock.Verify(broker =>
            broker.SelectKnowledgeAsync(randomQuery),
                Times.Once);

        this.knowledgeBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
