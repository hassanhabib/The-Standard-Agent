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
    public async Task ShouldReturnEmptyWhenBrokerReturnsNoSkillsAsync()
    {
        // given
        this.skillBrokerMock.Setup(broker =>
            broker.SelectSkillsAsync())
                .ReturnsAsync([]);

        // when
        string actualSkills = await this.skillService.RetrieveSkillsAsync();

        // then
        actualSkills.Should().BeEmpty();

        this.skillBrokerMock.Verify(broker =>
            broker.SelectSkillsAsync(),
                Times.Once);

        this.skillBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldComposeSkillsInNameOrderAsync()
    {
        // given
        string firstSkill = CreateRandomString();
        string secondSkill = CreateRandomString();

        var unorderedSkills = new List<Skill>
        {
            new() { Name = "b-second.md", Content = secondSkill },
            new() { Name = "a-first.md", Content = firstSkill }
        };

        string expectedSkills = string.Join("\n\n", firstSkill, secondSkill);

        this.skillBrokerMock.Setup(broker =>
            broker.SelectSkillsAsync())
                .ReturnsAsync(unorderedSkills);

        // when
        string actualSkills = await this.skillService.RetrieveSkillsAsync();

        // then — composed in Name order regardless of the order the broker returned
        actualSkills.Should().Be(expectedSkills);

        this.skillBrokerMock.Verify(broker =>
            broker.SelectSkillsAsync(),
                Times.Once);

        this.skillBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
