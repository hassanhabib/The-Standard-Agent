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
        // FileNotFound and DirectoryNotFound both derive from IOException, so they
        // must be caught above any IOException block or it would swallow them.
        catch (FileNotFoundException fileNotFoundException)
        {
            var failedSkillDependencyException =
                new FailedSkillDependencyException(
                    message: "Failed skill dependency error occurred, contact support.",
                    innerException: fileNotFoundException);

            throw await CreateAndLogCriticalDependencyExceptionAsync(
                failedSkillDependencyException);
        }
        catch (DirectoryNotFoundException directoryNotFoundException)
        {
            var failedSkillDependencyException =
                new FailedSkillDependencyException(
                    message: "Failed skill dependency error occurred, contact support.",
                    innerException: directoryNotFoundException);

            throw await CreateAndLogCriticalDependencyExceptionAsync(
                failedSkillDependencyException);
        }
        catch (UnauthorizedAccessException unauthorizedAccessException)
        {
            var failedSkillDependencyException =
                new FailedSkillDependencyException(
                    message: "Failed skill dependency error occurred, contact support.",
                    innerException: unauthorizedAccessException);

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
