// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Services.Foundations.Skills;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Skills;

public partial class SkillServiceTests
{
    [Fact]
    public async Task ShouldRetrieveOnlyRoutedSkillFileWhenRouteIsGivenAsync()
    {
        // given
        string skillsPath = CreateRandomString();
        string calculatorPath = "calculator-skill.md";
        string identityPath = "identity-skill.md";
        List<string> filePaths = [identityPath, calculatorPath];

        string calculatorSkill = CreateRandomString();
        string identitySkill = CreateRandomString();

        var fileSkillService = new SkillService(
            fileBroker: this.fileBrokerMock.Object,
            skillsPath: skillsPath,
            loggingBroker: this.loggingBrokerMock.Object);

        this.fileBrokerMock.Setup(broker =>
            broker.DirectoryExists(skillsPath))
                .Returns(true);

        this.fileBrokerMock.Setup(broker =>
            broker.SelectFiles(skillsPath, "*.md", SearchOption.TopDirectoryOnly))
                .Returns(filePaths);

        this.fileBrokerMock.Setup(broker =>
            broker.ReadFileAsync(calculatorPath))
                .ReturnsAsync(calculatorSkill);

        this.fileBrokerMock.Setup(broker =>
            broker.ReadFileAsync(identityPath))
                .ReturnsAsync(identitySkill);

        // when
        string actualSkills = await fileSkillService.RetrieveSkillsAsync(route: "calculator");

        // then — only the file whose name matches the route label is fanned in
        actualSkills.Should().Be(calculatorSkill);

        this.fileBrokerMock.Verify(broker =>
            broker.ReadFileAsync(calculatorPath),
                Times.Once);

        this.fileBrokerMock.Verify(broker =>
            broker.ReadFileAsync(identityPath),
                Times.Never);
    }
}
