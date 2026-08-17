// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Policys.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Foundations.Policys;

public partial class PolicyService
{
    private async ValueTask<T> TryCatch<T>(Func<ValueTask<T>> returningFunction)
    {
        try
        {
            return await returningFunction();
        }
        catch (InvalidPolicyException invalidException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidException);
        }
        catch (UnauthorizedAccessException unauthorizedAccessException)
        {
            throw await CreateAndLogCriticalDependencyExceptionAsync(unauthorizedAccessException);
        }
        catch (IOException ioException)
        {
            throw await CreateAndLogDependencyExceptionAsync(ioException);
        }
        catch (HttpRequestException httpRequestException)
        {
            throw await CreateAndLogDependencyExceptionAsync(httpRequestException);
        }
        catch (Exception exception)
        {
            var failedServiceException =
                new FailedPolicyServiceException(
                    message: "Failed policy service error occurred, contact support.",
                    innerException: exception);

            throw await CreateAndLogServiceExceptionAsync(failedServiceException);
        }
    }

    private async ValueTask TryCatch(Func<ValueTask> returningNothingFunction)
    {
        await TryCatch(async () =>
        {
            await returningNothingFunction();

            return true;
        });
    }

    private async ValueTask<PolicyValidationException> CreateAndLogValidationExceptionAsync(
        Xeption? exception)
    {
        var validationException =
            new PolicyValidationException(
                message: "Policy validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(validationException);

        return validationException;
    }

    private async ValueTask<PolicyDependencyException>
        CreateAndLogCriticalDependencyExceptionAsync(Exception exception)
    {
        var dependencyException = Wrap(exception);

        await this.loggingBroker.LogCriticalAsync(dependencyException);

        return dependencyException;
    }

    private async ValueTask<PolicyDependencyException> CreateAndLogDependencyExceptionAsync(
        Exception exception)
    {
        var dependencyException = Wrap(exception);

        await this.loggingBroker.LogErrorAsync(dependencyException);

        return dependencyException;
    }

    private static PolicyDependencyException Wrap(Exception exception) =>
        new(
            message: "Policy dependency error occurred, contact support.",
            innerException: new FailedPolicyDependencyException(
                message: "Failed policy dependency error occurred, contact support.",
                innerException: exception));

    private async ValueTask<PolicyServiceException> CreateAndLogServiceExceptionAsync(
        Xeption? exception)
    {
        var serviceException =
            new PolicyServiceException(
                message: "Policy service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(serviceException);

        return serviceException;
    }
}
