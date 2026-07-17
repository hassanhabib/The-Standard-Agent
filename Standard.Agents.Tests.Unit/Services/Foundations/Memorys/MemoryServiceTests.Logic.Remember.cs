// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Moq;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Memorys;

public partial class MemoryServiceTests
{
    [Fact]
    public async Task ShouldRememberAsync()
    {
        // given
        string randomMemory = CreateRandomString();
        string inputMemory = randomMemory;

        // when
        await this.memoryService.RememberAsync(inputMemory);

        // then
        this.memoryBrokerMock.Verify(broker =>
            broker.InsertMemoryAsync(inputMemory),
                Times.Once);

        this.memoryBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
