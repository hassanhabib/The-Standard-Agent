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
}
