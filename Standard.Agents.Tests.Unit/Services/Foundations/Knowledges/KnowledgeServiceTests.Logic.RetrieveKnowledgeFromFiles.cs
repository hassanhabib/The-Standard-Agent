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
}
