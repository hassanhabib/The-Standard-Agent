// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Linq;
using Moq;
using Standard.Agents.Services.Foundations.Memorys;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Memorys;

public partial class MemoryServiceTests
{
    [Fact]
    public async Task ShouldRememberByAppendingToMemoryFileAsync()
    {
        // given
        string memoryDirectory = CreateRandomString();
        string memoryPath = Path.Combine(memoryDirectory, CreateRandomString() + ".txt");
        string inputMemory = CreateRandomString();

        var fileMemoryService = new MemoryService(
            fileBroker: this.fileBrokerMock.Object,
            memoryPath: memoryPath,
            loggingBroker: this.loggingBrokerMock.Object);

        // when
        await fileMemoryService.RememberAsync(inputMemory);

        // then
        this.fileBrokerMock.Verify(broker =>
            broker.CreateDirectory(memoryDirectory),
                Times.Once);

        this.fileBrokerMock.Verify(broker =>
            broker.AppendAllLinesAsync(
                memoryPath,
                It.Is<IEnumerable<string>>(lines => lines.Single() == inputMemory)),
                    Times.Once);

        this.fileBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
