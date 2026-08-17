// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Moq;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Coordinations.DataNature;

public partial class DataCoordinationServiceTests
{
    [Fact]
    public async Task ShouldRememberAsync()
    {
        // given
        string randomMemory = CreateRandomString();

        // when
        await this.dataCoordinationService.RememberAsync(randomMemory);

        // then
        this.memoryServiceMock.Verify(service =>
            service.RememberAsync(randomMemory),
                Times.Once);

        this.memoryServiceMock.VerifyNoOtherCalls();
    }
}
