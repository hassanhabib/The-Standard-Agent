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

    [Fact]
    public async Task ShouldRetrieveMatchingKnowledgeFromOrderedFilesUpToMaxResultsAsync()
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

        string firstDocument = "alpha needle";        // matches
        string secondDocument = "beta";               // no match, skipped
        string thirdDocument = "gamma NEEDLE";         // matches (case-insensitive) -> reaches maxResults
        string fourthDocument = "delta needle";        // matches but never read (max already reached)

        IReadOnlyList<string> expectedDocuments = [firstDocument, thirdDocument];

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

        // then
        actualDocuments.Should().Equal(expectedDocuments);

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
                Times.Never);

        this.fileBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
