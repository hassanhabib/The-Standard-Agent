// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Data;

public partial class SkillServiceTests
{
    [Fact]
    public async Task ShouldRetrieveSkillsAsync()
    {
        // given
        string randomSkills = CreateRandomString();
        string retrievedSkills = randomSkills;
        string expectedSkills = retrievedSkills;

        this.skillBrokerMock.Setup(broker =>
            broker.SelectSkillsAsync())
                .ReturnsAsync(retrievedSkills);

        // when
        string actualSkills = await this.skillService.RetrieveSkillsAsync();

        // then
        actualSkills.Should().BeEquivalentTo(expectedSkills);

        this.skillBrokerMock.Verify(broker =>
            broker.SelectSkillsAsync(),
                Times.Once);

        this.skillBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
