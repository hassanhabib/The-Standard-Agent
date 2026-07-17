// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Skills.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Foundations.Data;

public partial class SkillService
{
    private delegate ValueTask<string> ReturningStringFunction();

    private async ValueTask<string> TryCatch(ReturningStringFunction returningStringFunction)
    {
        try
        {
            return await returningStringFunction();
        }
        catch (FileNotFoundException fileNotFoundException)
        {
            var failedSkillDependencyException =
                new FailedSkillDependencyException(
                    message: "Failed skill dependency error occurred, contact support.",
                    innerException: fileNotFoundException);

            throw await CreateAndLogCriticalDependencyExceptionAsync(
                failedSkillDependencyException);
        }
    }

    private async ValueTask<SkillDependencyException> CreateAndLogCriticalDependencyExceptionAsync(
        Xeption exception)
    {
        var skillDependencyException =
            new SkillDependencyException(
                message: "Skill dependency error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogCriticalAsync(skillDependencyException);

        return skillDependencyException;
    }
}
