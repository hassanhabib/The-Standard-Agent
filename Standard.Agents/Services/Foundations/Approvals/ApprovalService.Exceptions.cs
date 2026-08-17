// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Approvals.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Foundations.Approvals;

public partial class ApprovalService
{
    private async ValueTask<T> TryCatch<T>(Func<ValueTask<T>> returningFunction)
    {
        try
        {
            return await returningFunction();
        }
        catch (InvalidApprovalException invalidException)
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
                new FailedApprovalServiceException(
                    message: "Failed approval service error occurred, contact support.",
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

    private async ValueTask<ApprovalValidationException> CreateAndLogValidationExceptionAsync(
        Xeption? exception)
    {
        var validationException =
            new ApprovalValidationException(
                message: "Approval validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(validationException);

        return validationException;
    }

    private async ValueTask<ApprovalDependencyException>
        CreateAndLogCriticalDependencyExceptionAsync(Exception exception)
    {
        var dependencyException = Wrap(exception);

        await this.loggingBroker.LogCriticalAsync(dependencyException);

        return dependencyException;
    }

    private async ValueTask<ApprovalDependencyException> CreateAndLogDependencyExceptionAsync(
        Exception exception)
    {
        var dependencyException = Wrap(exception);

        await this.loggingBroker.LogErrorAsync(dependencyException);

        return dependencyException;
    }

    private static ApprovalDependencyException Wrap(Exception exception) =>
        new(
            message: "Approval dependency error occurred, contact support.",
            innerException: new FailedApprovalDependencyException(
                message: "Failed approval dependency error occurred, contact support.",
                innerException: exception));

    private async ValueTask<ApprovalServiceException> CreateAndLogServiceExceptionAsync(
        Xeption? exception)
    {
        var serviceException =
            new ApprovalServiceException(
                message: "Approval service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(serviceException);

        return serviceException;
    }
}
