// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Services.Foundations.Memorys;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Memorys;

public partial class MemoryServiceTests
{
    [Fact]
    public async Task ShouldReturnEmptyMemoriesIfMemoryFileDoesNotExistAsync()
    {
        // given
        string memoryPath = CreateRandomString();

        var fileMemoryService = new MemoryService(
            fileBroker: this.fileBrokerMock.Object,
            memoryPath: memoryPath,
            loggingBroker: this.loggingBrokerMock.Object);

        this.fileBrokerMock.Setup(broker =>
            broker.FileExists(memoryPath))
                .Returns(false);

        // when
        IReadOnlyList<string> actualMemories =
            await fileMemoryService.RecallMemoriesAsync();

        // then
        actualMemories.Should().BeEmpty();

        this.fileBrokerMock.Verify(broker =>
            broker.FileExists(memoryPath),
                Times.Once);

        this.fileBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
