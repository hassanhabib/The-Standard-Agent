// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Skills;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Skills;

public partial class SkillServiceTests
{
    [Fact]
    public async Task ShouldRetrieveSkillsAsync()
    {
        // given
        string skillContent = CreateRandomString();

        var retrievedSkills = new List<Skill>
        {
            new() { Name = "00-skill.md", Content = skillContent }
        };

        string expectedSkills = skillContent;

        this.skillBrokerMock.Setup(broker =>
            broker.SelectSkillsAsync())
                .ReturnsAsync(retrievedSkills);

        // when
        string actualSkills = await this.skillService.RetrieveSkillsAsync();

        // then
        actualSkills.Should().Be(expectedSkills);

        this.skillBrokerMock.Verify(broker =>
            broker.SelectSkillsAsync(),
                Times.Once);

        this.skillBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
