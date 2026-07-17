// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Data;

public partial class MemoryServiceTests
{
    [Fact]
    public async Task ShouldRecallMemoriesAsync()
    {
        // given
        List<string> randomMemories = CreateRandomMemories();
        IReadOnlyList<string> retrievedMemories = randomMemories;
        IReadOnlyList<string> expectedMemories = retrievedMemories;

        this.memoryBrokerMock.Setup(broker =>
            broker.SelectMemoriesAsync())
                .ReturnsAsync(retrievedMemories);

        // when
        IReadOnlyList<string> actualMemories =
            await this.memoryService.RecallMemoriesAsync();

        // then
        actualMemories.Should().BeEquivalentTo(expectedMemories);

        this.memoryBrokerMock.Verify(broker =>
            broker.SelectMemoriesAsync(),
                Times.Once);

        this.memoryBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

            [Fact]
    public async Task ShouldRecallNoMemoriesOnRecallMemoriesIfStoreIsEmptyAsync()
    {
        // given
        IReadOnlyList<string> noMemories = [];
        IReadOnlyList<string> expectedMemories = noMemories;

        this.memoryBrokerMock.Setup(broker =>
            broker.SelectMemoriesAsync())
                .ReturnsAsync(noMemories);

        // when
        IReadOnlyList<string> actualMemories =
            await this.memoryService.RecallMemoriesAsync();

        // then
        actualMemories.Should().BeEmpty();
        actualMemories.Should().BeEquivalentTo(expectedMemories);

        this.memoryBrokerMock.Verify(broker =>
            broker.SelectMemoriesAsync(),
                Times.Once);

        this.memoryBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
