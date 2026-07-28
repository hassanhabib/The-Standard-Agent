// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Moq;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Data;

public partial class DataOrchestrationServiceTests
{
    [Fact]
    public async Task ShouldRememberAsync()
    {
        // given
        string randomMemory = CreateRandomString();

        // when
        await this.dataOrchestrationService.RememberAsync(randomMemory);

        // then
        this.memoryServiceMock.Verify(service =>
            service.RememberAsync(randomMemory),
                Times.Once);

        this.memoryServiceMock.VerifyNoOtherCalls();
    }
}
