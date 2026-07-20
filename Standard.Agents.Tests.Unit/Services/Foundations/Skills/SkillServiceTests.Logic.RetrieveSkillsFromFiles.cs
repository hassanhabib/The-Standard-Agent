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
    public async Task ShouldReturnEmptySkillsIfSkillsDirectoryDoesNotExistAsync()
    {
        // given
        string skillsPath = CreateRandomString();

        var fileSkillService = new SkillService(
            fileBroker: this.fileBrokerMock.Object,
            skillsPath: skillsPath,
            loggingBroker: this.loggingBrokerMock.Object);

        this.fileBrokerMock.Setup(broker =>
            broker.DirectoryExists(skillsPath))
                .Returns(false);

        // when
        string actualSkills = await fileSkillService.RetrieveSkillsAsync();

        // then
        actualSkills.Should().BeEmpty();

        this.fileBrokerMock.Verify(broker =>
            broker.DirectoryExists(skillsPath),
                Times.Once);

        this.fileBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldRetrieveSkillsFromOrderedMarkdownFilesAsync()
    {
        // given
        string skillsPath = CreateRandomString();
        string firstFilePath = "a-" + CreateRandomString() + ".md";
        string secondFilePath = "b-" + CreateRandomString() + ".md";
        List<string> unorderedFilePaths = [secondFilePath, firstFilePath];

        string firstSkill = CreateRandomString();
        string secondSkill = CreateRandomString();
        string expectedSkills = string.Join("\n\n", firstSkill, secondSkill);

        var fileSkillService = new SkillService(
            fileBroker: this.fileBrokerMock.Object,
            skillsPath: skillsPath,
            loggingBroker: this.loggingBrokerMock.Object);

        this.fileBrokerMock.Setup(broker =>
            broker.DirectoryExists(skillsPath))
                .Returns(true);

        this.fileBrokerMock.Setup(broker =>
            broker.SelectFiles(skillsPath, "*.md", SearchOption.TopDirectoryOnly))
                .Returns(unorderedFilePaths);

        this.fileBrokerMock.Setup(broker =>
            broker.ReadFileAsync(firstFilePath))
                .ReturnsAsync(firstSkill);

        this.fileBrokerMock.Setup(broker =>
            broker.ReadFileAsync(secondFilePath))
                .ReturnsAsync(secondSkill);

        // when
        string actualSkills = await fileSkillService.RetrieveSkillsAsync();

        // then
        actualSkills.Should().Be(expectedSkills);

        this.fileBrokerMock.Verify(broker =>
            broker.DirectoryExists(skillsPath),
                Times.Once);

        this.fileBrokerMock.Verify(broker =>
            broker.SelectFiles(skillsPath, "*.md", SearchOption.TopDirectoryOnly),
                Times.Once);

        this.fileBrokerMock.Verify(broker =>
            broker.ReadFileAsync(firstFilePath),
                Times.Once);

        this.fileBrokerMock.Verify(broker =>
            broker.ReadFileAsync(secondFilePath),
                Times.Once);

        this.fileBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
