// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Returns;

public partial class ReturnServiceTests
{
    [Fact]
    public async Task ShouldReturnAsync()
    {
        // given
        string randomPayload = CreateRandomString();
        string inputPayload = randomPayload;
        string expectedPayload = inputPayload;

        // when
        string actualPayload = await this.returnService.ReturnAsync(inputPayload);

        // then
        actualPayload.Should().BeEquivalentTo(expectedPayload);

        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
