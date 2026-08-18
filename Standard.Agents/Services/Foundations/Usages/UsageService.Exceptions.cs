// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Usages;
using Standard.Agents.Models.Foundations.Usages.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Foundations.Usages;

public partial class UsageService
{
    private delegate ValueTask<AgentUsage> ReturningUsageFunction();

    private async ValueTask<AgentUsage> TryCatch(ReturningUsageFunction returningUsageFunction)
    {
        try
        {
            return await returningUsageFunction();
        }
        catch (InvalidUsageException invalidUsageException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidUsageException);
        }

        // A counter that cannot be reached is a dependency failure, not a service one — the
        // distinction matters because the caller can carry on with a reported number, and only
        // knows to if the failure names the counter rather than the measurement.
        catch (HttpRequestException httpRequestException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                new FailedUsageDependencyException(
                    message: "Failed usage dependency error occurred, contact support.",
                    innerException: httpRequestException));
        }
        catch (TaskCanceledException taskCanceledException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                new FailedUsageDependencyException(
                    message: "Failed usage dependency error occurred, contact support.",
                    innerException: taskCanceledException));
        }
        catch (Exception exception)
        {
            var failedUsageServiceException =
                new FailedUsageServiceException(
                    message: "Failed usage service error occurred, contact support.",
                    innerException: exception);

            throw await CreateAndLogServiceExceptionAsync(failedUsageServiceException);
        }
    }

    private async ValueTask<UsageValidationException> CreateAndLogValidationExceptionAsync(
        Xeption exception)
    {
        var usageValidationException =
            new UsageValidationException(
                message: "Usage validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(usageValidationException);

        return usageValidationException;
    }

    private async ValueTask<UsageDependencyException> CreateAndLogDependencyExceptionAsync(
        Xeption exception)
    {
        var usageDependencyException =
            new UsageDependencyException(
                message: "Usage dependency error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(usageDependencyException);

        return usageDependencyException;
    }

    private async ValueTask<UsageServiceException> CreateAndLogServiceExceptionAsync(
        Xeption exception)
    {
        var usageServiceException =
            new UsageServiceException(
                message: "Usage service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(usageServiceException);

        return usageServiceException;
    }
}
