// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Force.DeepCloner;
using Moq;
using Standard.Agents.Models.Foundations.Knowledges;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Knowledges;

public partial class KnowledgeServiceTests
{
    [Fact]
    public async Task ShouldRetrieveSourcedKnowledgeAsync()
    {
        // given
        string randomQuery = CreateRandomString();
        List<KnowledgeResult> randomKnowledgeResults = CreateRandomKnowledgeResults();
        string inputQuery = randomQuery;
        IReadOnlyList<KnowledgeResult> selectedKnowledgeResults = randomKnowledgeResults;

        IReadOnlyList<KnowledgeResult> expectedKnowledgeResults =
            selectedKnowledgeResults.DeepClone();

        this.sourcedKnowledgeBrokerMock.Setup(broker =>
            broker.SelectSourcedKnowledgeAsync(inputQuery))
                .ReturnsAsync(selectedKnowledgeResults);

        // when
        IReadOnlyList<KnowledgeResult> actualKnowledgeResults =
            await this.sourcedKnowledgeService.RetrieveSourcedKnowledgeAsync(inputQuery);

        // then
        actualKnowledgeResults.Should().BeEquivalentTo(
            expectedKnowledgeResults,
            options => options.WithStrictOrdering());

        this.sourcedKnowledgeBrokerMock.Verify(broker =>
            broker.SelectSourcedKnowledgeAsync(inputQuery),
                Times.Once);

        this.sourcedKnowledgeBrokerMock.VerifyNoOtherCalls();
        this.knowledgeBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    // A plain broker never said where a passage came from, and lifting it must not pretend it
    // did (SPEC.md §4.1): the passage arrives with no score and no source, which is exactly what
    // it always was, and a passage with no source is never cited.
    [Fact]
    public async Task ShouldRetrieveUnsourcedKnowledgeOnRetrieveSourcedKnowledgeIfTheBrokerCannotSayWhereItCameFromAsync()
    {
        // given
        string randomQuery = CreateRandomString();
        List<string> randomDocuments = CreateRandomDocuments();
        string inputQuery = randomQuery;
        IReadOnlyList<string> selectedDocuments = randomDocuments;

        IReadOnlyList<KnowledgeResult> expectedKnowledgeResults =
            selectedDocuments.Select(document => new KnowledgeResult
            {
                Text = document,
                Score = null,
                Source = string.Empty
            })
                .ToList();

        this.knowledgeBrokerMock.Setup(broker =>
            broker.SelectKnowledgeAsync(inputQuery))
                .ReturnsAsync(selectedDocuments);

        // when
        IReadOnlyList<KnowledgeResult> actualKnowledgeResults =
            await this.knowledgeService.RetrieveSourcedKnowledgeAsync(inputQuery);

        // then
        actualKnowledgeResults.Should().BeEquivalentTo(
            expectedKnowledgeResults,
            options => options.WithStrictOrdering());

        this.knowledgeBrokerMock.Verify(broker =>
            broker.SelectKnowledgeAsync(inputQuery),
                Times.Once);

        this.knowledgeBrokerMock.VerifyNoOtherCalls();
        this.sourcedKnowledgeBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    // The plain door over a sourced broker: the same passages, in the same order, with nothing
    // beside them. A caller that never asked where a passage came from sees what it always saw.
    [Fact]
    public async Task ShouldRetrieveTheTextsOfSourcedKnowledgeOnRetrieveKnowledgeAsync()
    {
        // given
        string randomQuery = CreateRandomString();
        List<KnowledgeResult> randomKnowledgeResults = CreateRandomKnowledgeResults();
        string inputQuery = randomQuery;
        IReadOnlyList<KnowledgeResult> selectedKnowledgeResults = randomKnowledgeResults;

        IReadOnlyList<string> expectedDocuments =
            selectedKnowledgeResults.Select(knowledgeResult => knowledgeResult.Text)
                .ToList();

        this.sourcedKnowledgeBrokerMock.Setup(broker =>
            broker.SelectSourcedKnowledgeAsync(inputQuery))
                .ReturnsAsync(selectedKnowledgeResults);

        // when
        IReadOnlyList<string> actualDocuments =
            await this.sourcedKnowledgeService.RetrieveKnowledgeAsync(inputQuery);

        // then
        actualDocuments.Should().BeEquivalentTo(
            expectedDocuments,
            options => options.WithStrictOrdering());

        this.sourcedKnowledgeBrokerMock.Verify(broker =>
            broker.SelectSourcedKnowledgeAsync(inputQuery),
                Times.Once);

        this.sourcedKnowledgeBrokerMock.VerifyNoOtherCalls();
        this.knowledgeBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
