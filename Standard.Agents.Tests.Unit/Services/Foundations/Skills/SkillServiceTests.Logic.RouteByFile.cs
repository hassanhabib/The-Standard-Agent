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
    public async Task ShouldRetrieveOnlyRoutedSkillWhenRouteIsGivenAsync()
    {
        // given
        string calculatorSkill = CreateRandomString();
        string identitySkill = CreateRandomString();

        var skills = new List<Skill>
        {
            new() { Name = "identity-skill.md", Content = identitySkill },
            new() { Name = "calculator-skill.md", Content = calculatorSkill }
        };

        this.skillBrokerMock.Setup(broker =>
            broker.SelectSkillsAsync())
                .ReturnsAsync(skills);

        // when
        string actualSkills =
            await this.skillService.RetrieveSkillsAsync(route: "calculator");

        // then — only the skill whose name matches the route label is fanned in
        actualSkills.Should().Be(calculatorSkill);
    }

    [Fact]
    public async Task ShouldRouteByNestedFolderNameWhenRouteIsGivenAsync()
    {
        // given — a nested skill is identified by its folder in Name
        string nestedSkill = CreateRandomString();
        string otherSkill = CreateRandomString();

        var skills = new List<Skill>
        {
            new() { Name = "the-standard-skill/SKILL.md", Content = nestedSkill },
            new() { Name = "other-skill.md", Content = otherSkill }
        };

        this.skillBrokerMock.Setup(broker =>
            broker.SelectSkillsAsync())
                .ReturnsAsync(skills);

        // when
        string actualSkills =
            await this.skillService.RetrieveSkillsAsync(route: "the-standard-skill");

        // then
        actualSkills.Should().Be(nestedSkill);
    }
}
