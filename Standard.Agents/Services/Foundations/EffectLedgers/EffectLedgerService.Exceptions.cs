// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.EffectLedgers.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Foundations.EffectLedgers;

public partial class EffectLedgerService
{
    private async ValueTask<T> TryCatch<T>(Func<ValueTask<T>> returningFunction)
    {
        try
        {
            return await returningFunction();
        }
        catch (InvalidEffectLedgerException invalidException)
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
                new FailedEffectLedgerServiceException(
                    message: "Failed effect ledger service error occurred, contact support.",
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

    private async ValueTask<EffectLedgerValidationException> CreateAndLogValidationExceptionAsync(
        Xeption? exception)
    {
        var validationException =
            new EffectLedgerValidationException(
                message: "Effect ledger validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(validationException);

        return validationException;
    }

    private async ValueTask<EffectLedgerDependencyException>
        CreateAndLogCriticalDependencyExceptionAsync(Exception exception)
    {
        var dependencyException = Wrap(exception);

        await this.loggingBroker.LogCriticalAsync(dependencyException);

        return dependencyException;
    }

    private async ValueTask<EffectLedgerDependencyException> CreateAndLogDependencyExceptionAsync(
        Exception exception)
    {
        var dependencyException = Wrap(exception);

        await this.loggingBroker.LogErrorAsync(dependencyException);

        return dependencyException;
    }

    private static EffectLedgerDependencyException Wrap(Exception exception) =>
        new(
            message: "Effect ledger dependency error occurred, contact support.",
            innerException: new FailedEffectLedgerDependencyException(
                message: "Failed effect ledger dependency error occurred, contact support.",
                innerException: exception));

    private async ValueTask<EffectLedgerServiceException> CreateAndLogServiceExceptionAsync(
        Xeption? exception)
    {
        var serviceException =
            new EffectLedgerServiceException(
                message: "Effect ledger service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(serviceException);

        return serviceException;
    }
}
