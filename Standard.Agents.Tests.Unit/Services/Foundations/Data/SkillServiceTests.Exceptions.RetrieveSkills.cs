// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Foundations.Skills.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Data;

public partial class SkillServiceTests
{
    [Fact]
    public async Task ShouldThrowDependencyExceptionOnRetrieveSkillsIfFileNotFoundErrorOccursAndLogItAsync()
    {
        // given
        var fileNotFoundException = new FileNotFoundException();

        var failedSkillDependencyException =
            new FailedSkillDependencyException(
                message: "Failed skill dependency error occurred, contact support.",
                innerException: fileNotFoundException);

        var expectedSkillDependencyException =
            new SkillDependencyException(
                message: "Skill dependency error occurred, contact support.",
                innerException: failedSkillDependencyException);

        this.skillBrokerMock.Setup(broker =>
            broker.SelectSkillsAsync())
                .ThrowsAsync(fileNotFoundException);

        // when
        ValueTask<string> retrieveSkillsTask =
            this.skillService.RetrieveSkillsAsync();

        SkillDependencyException actualSkillDependencyException =
            await Assert.ThrowsAsync<SkillDependencyException>(
                retrieveSkillsTask.AsTask);

        // then
        actualSkillDependencyException.Should()
            .BeEquivalentTo(expectedSkillDependencyException);

        this.skillBrokerMock.Verify(broker =>
            broker.SelectSkillsAsync(),
                Times.Once);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(
                expectedSkillDependencyException))),
                    Times.Once);

        this.skillBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
