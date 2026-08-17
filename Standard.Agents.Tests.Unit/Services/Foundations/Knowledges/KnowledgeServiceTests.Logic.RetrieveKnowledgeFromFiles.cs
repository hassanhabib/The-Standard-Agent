// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Services.Foundations.Knowledges;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Knowledges;

public partial class KnowledgeServiceTests
{
    [Fact]
    public async Task ShouldReturnEmptyKnowledgeIfKnowledgeDirectoryDoesNotExistAsync()
    {
        // given
        string knowledgePath = CreateRandomString();
        string searchPattern = "*.md";
        int maxResults = 3;
        string query = CreateRandomString();

        var fileKnowledgeService = new KnowledgeService(
            fileBroker: this.fileBrokerMock.Object,
            knowledgePath: knowledgePath,
            searchPattern: searchPattern,
            maxResults: maxResults,
            loggingBroker: this.loggingBrokerMock.Object);

        this.fileBrokerMock.Setup(broker =>
            broker.DirectoryExists(knowledgePath))
                .Returns(false);

        // when
        IReadOnlyList<string> actualDocuments =
            await fileKnowledgeService.RetrieveKnowledgeAsync(query);

        // then
        actualDocuments.Should().BeEmpty();

        this.fileBrokerMock.Verify(broker =>
            broker.DirectoryExists(knowledgePath),
                Times.Once);

        this.fileBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    // Ranking replaced first-N-found (SPEC.md §4.2), and the difference is visible here: every
    // document is read, because relevance is relative — a term's weight depends on how rare it
    // is across the corpus, which is not knowable from a prefix of it. The old contract stopped
    // reading once it had enough matches, and so could only ever return the first ones found.
    [Fact]
    public async Task ShouldRetrieveTheMostRelevantKnowledgeUpToMaxResultsAsync()
    {
        // given
        string knowledgePath = CreateRandomString();
        string searchPattern = "*.md";
        int maxResults = 2;
        string query = "needle";

        string firstPath = "a.md";
        string secondPath = "b.md";
        string thirdPath = "c.md";
        string fourthPath = "d.md";
        List<string> unorderedPaths = [thirdPath, firstPath, fourthPath, secondPath];

        string firstDocument = "alpha needle";
        string secondDocument = "beta";                // carries no query term, scores zero
        string thirdDocument = "gamma NEEDLE";          // case-insensitive
        string fourthDocument = "delta needle";

        var fileKnowledgeService = new KnowledgeService(
            fileBroker: this.fileBrokerMock.Object,
            knowledgePath: knowledgePath,
            searchPattern: searchPattern,
            maxResults: maxResults,
            loggingBroker: this.loggingBrokerMock.Object);

        this.fileBrokerMock.Setup(broker =>
            broker.DirectoryExists(knowledgePath))
                .Returns(true);

        this.fileBrokerMock.Setup(broker =>
            broker.SelectFiles(knowledgePath, searchPattern, SearchOption.AllDirectories))
                .Returns(unorderedPaths);

        this.fileBrokerMock.Setup(broker =>
            broker.ReadFileAsync(firstPath))
                .ReturnsAsync(firstDocument);

        this.fileBrokerMock.Setup(broker =>
            broker.ReadFileAsync(secondPath))
                .ReturnsAsync(secondDocument);

        this.fileBrokerMock.Setup(broker =>
            broker.ReadFileAsync(thirdPath))
                .ReturnsAsync(thirdDocument);

        this.fileBrokerMock.Setup(broker =>
            broker.ReadFileAsync(fourthPath))
                .ReturnsAsync(fourthDocument);

        // when
        IReadOnlyList<string> actualDocuments =
            await fileKnowledgeService.RetrieveKnowledgeAsync(query);

        // then — the best matches, capped, and never the document carrying no query term
        actualDocuments.Should().HaveCount(maxResults);
        actualDocuments.Should().NotContain(secondDocument);

        actualDocuments.Should().OnlyContain(document =>
            document.Contains("needle", StringComparison.OrdinalIgnoreCase));

        this.fileBrokerMock.Verify(broker =>
            broker.DirectoryExists(knowledgePath),
                Times.Once);

        this.fileBrokerMock.Verify(broker =>
            broker.SelectFiles(knowledgePath, searchPattern, SearchOption.AllDirectories),
                Times.Once);

        this.fileBrokerMock.Verify(broker =>
            broker.ReadFileAsync(firstPath),
                Times.Once);

        this.fileBrokerMock.Verify(broker =>
            broker.ReadFileAsync(secondPath),
                Times.Once);

        this.fileBrokerMock.Verify(broker =>
            broker.ReadFileAsync(thirdPath),
                Times.Once);

        this.fileBrokerMock.Verify(broker =>
            broker.ReadFileAsync(fourthPath),
                Times.Once);

        this.fileBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
