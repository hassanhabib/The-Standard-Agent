// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Knowledges;
using Standard.Agents.Services.Foundations.Knowledges;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Knowledges;

public partial class KnowledgeServiceTests
{
    // The knowledge an agent is given with nothing installed is citable out of the box (SPEC.md
    // §4.2): each passage names the document it was cut from, relative to the knowledge folder and
    // with forward slashes whatever the platform, so a citation reads the same on every machine.
    [Fact]
    public async Task ShouldRetrieveSourcedKnowledgeFromFilesWithTheirPathsAndScoresAsync()
    {
        // given
        string knowledgePath = Path.Combine(Path.GetTempPath(), CreateRandomString());
        string searchPattern = "*.md";
        int maxResults = 3;
        string query = "refund";
        string refundsPath = Path.Combine(knowledgePath, "policies", "refunds.md");
        string shippingPath = Path.Combine(knowledgePath, "shipping.md");
        List<string> documentPaths = [shippingPath, refundsPath];
        string refundsDocument = "Enterprise customers may request a refund within 90 days.";
        string shippingDocument = "Orders ship within two business days.";

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
                .Returns(documentPaths);

        this.fileBrokerMock.Setup(broker =>
            broker.ReadFileAsync(refundsPath))
                .ReturnsAsync(refundsDocument);

        this.fileBrokerMock.Setup(broker =>
            broker.ReadFileAsync(shippingPath))
                .ReturnsAsync(shippingDocument);

        // when
        IReadOnlyList<KnowledgeResult> actualKnowledgeResults =
            await fileKnowledgeService.RetrieveSourcedKnowledgeAsync(query);

        // then — only the passage carrying the query term, credited to its document, scored
        actualKnowledgeResults.Should().ContainSingle();
        actualKnowledgeResults[0].Text.Should().Be(refundsDocument);
        actualKnowledgeResults[0].Source.Should().Be("policies/refunds.md");
        actualKnowledgeResults[0].Score.Should().BeGreaterThan(0);

        this.fileBrokerMock.Verify(broker =>
            broker.DirectoryExists(knowledgePath),
                Times.Once);

        this.fileBrokerMock.Verify(broker =>
            broker.SelectFiles(knowledgePath, searchPattern, SearchOption.AllDirectories),
                Times.Once);

        this.fileBrokerMock.Verify(broker =>
            broker.ReadFileAsync(refundsPath),
                Times.Once);

        this.fileBrokerMock.Verify(broker =>
            broker.ReadFileAsync(shippingPath),
                Times.Once);

        this.fileBrokerMock.VerifyNoOtherCalls();
        this.knowledgeBrokerMock.VerifyNoOtherCalls();
        this.sourcedKnowledgeBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
