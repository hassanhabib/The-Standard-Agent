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
        catch (IOException ioException)
        {
            var failedSkillDependencyException =
                new FailedSkillDependencyException(
                    message: "Failed skill dependency error occurred, contact support.",
                    innerException: ioException);

            throw await CreateAndLogDependencyExceptionAsync(
                failedSkillDependencyException);
        }
        catch (Exception exception)
        {
            var failedSkillServiceException =
                new FailedSkillServiceException(
                    message: "Failed skill service error occurred, contact support.",
                    innerException: exception);

            throw await CreateAndLogServiceExceptionAsync(
                failedSkillServiceException);
        }
    }

    private async ValueTask<SkillDependencyException> CreateAndLogCriticalDependencyExceptionAsync(
        Xeption? exception)
    {
        var skillDependencyException =
            new SkillDependencyException(
                message: "Skill dependency error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogCriticalAsync(skillDependencyException);

        return skillDependencyException;
    }

    private async ValueTask<SkillDependencyException> CreateAndLogDependencyExceptionAsync(
        Xeption? exception)
    {
        var skillDependencyException =
            new SkillDependencyException(
                message: "Skill dependency error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(skillDependencyException);

        return skillDependencyException;
    }

    private async ValueTask<SkillServiceException> CreateAndLogServiceExceptionAsync(
        Xeption? exception)
    {
        var skillServiceException =
            new SkillServiceException(
                message: "Skill service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(skillServiceException);

        return skillServiceException;
    }
}
