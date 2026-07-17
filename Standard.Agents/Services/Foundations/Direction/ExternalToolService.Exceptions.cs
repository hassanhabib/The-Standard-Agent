// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using RESTFulSense.Exceptions;
using Standard.Agents.Models.Foundations.ExternalTools.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Foundations.Direction;

public partial class ExternalToolService
{
    private delegate ValueTask<string> ReturningStringFunction();

                private async ValueTask<string> TryCatch(ReturningStringFunction returningStringFunction)
    {
        try
        {
            return await returningStringFunction();
        }
        catch (InvalidExternalToolException invalidExternalToolException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidExternalToolException);
        }
        catch (HttpResponseBadRequestException httpResponseBadRequestException)
        {
            var invalidExternalToolException =
                new InvalidExternalToolException(
                    message: "Invalid external tool request. Please correct the error and try again.");

            invalidExternalToolException.AddData(httpResponseBadRequestException.Data);

            throw await CreateAndLogDependencyValidationExceptionAsync(invalidExternalToolException);
        }
        catch (HttpResponseUnauthorizedException httpResponseUnauthorizedException)
        {
            throw await CreateAndLogCriticalDependencyExceptionAsync(
                httpResponseUnauthorizedException);
        }
        catch (HttpResponseForbiddenException httpResponseForbiddenException)
        {
            throw await CreateAndLogCriticalDependencyExceptionAsync(
                httpResponseForbiddenException);
        }
        catch (HttpResponseNotFoundException httpResponseNotFoundException)
        {
            throw await CreateAndLogCriticalDependencyExceptionAsync(
                httpResponseNotFoundException);
        }
        catch (HttpResponseUrlNotFoundException httpResponseUrlNotFoundException)
        {
            throw await CreateAndLogCriticalDependencyExceptionAsync(
                httpResponseUrlNotFoundException);
        }
        catch (HttpResponseInternalServerErrorException httpResponseInternalServerErrorException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                httpResponseInternalServerErrorException);
        }
        catch (HttpResponseServiceUnavailableException httpResponseServiceUnavailableException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                httpResponseServiceUnavailableException);
        }
        catch (HttpRequestException httpRequestException)
        {
            throw await CreateAndLogCriticalDependencyExceptionAsync(httpRequestException);
        }
        catch (Exception exception)
        {
            var failedExternalToolServiceException =
                new FailedExternalToolServiceException(
                    message: "Failed external tool service error occurred, contact support.",
                    innerException: exception);

            throw await CreateAndLogServiceExceptionAsync(failedExternalToolServiceException);
        }
    }

    private async ValueTask<ExternalToolValidationException> CreateAndLogValidationExceptionAsync(
        Xeption? exception)
    {
        var externalToolValidationException =
            new ExternalToolValidationException(
                message: "External tool validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(externalToolValidationException);

        return externalToolValidationException;
    }

    private async ValueTask<ExternalToolDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
        Xeption? exception)
    {
        var externalToolDependencyValidationException =
            new ExternalToolDependencyValidationException(
                message: "External tool dependency validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(externalToolDependencyValidationException);

        return externalToolDependencyValidationException;
    }

    private async ValueTask<ExternalToolDependencyException> CreateAndLogCriticalDependencyExceptionAsync(
        Exception exception)
    {
        var failedExternalToolDependencyException =
            new FailedExternalToolDependencyException(
                message: "Failed external tool dependency error occurred, contact support.",
                innerException: exception);

        var externalToolDependencyException =
            new ExternalToolDependencyException(
                message: "External tool dependency error occurred, contact support.",
                innerException: failedExternalToolDependencyException);

        await this.loggingBroker.LogCriticalAsync(externalToolDependencyException);

        return externalToolDependencyException;
    }

    private async ValueTask<ExternalToolDependencyException> CreateAndLogDependencyExceptionAsync(
        Exception exception)
    {
        var failedExternalToolDependencyException =
            new FailedExternalToolDependencyException(
                message: "Failed external tool dependency error occurred, contact support.",
                innerException: exception);

        var externalToolDependencyException =
            new ExternalToolDependencyException(
                message: "External tool dependency error occurred, contact support.",
                innerException: failedExternalToolDependencyException);

        await this.loggingBroker.LogErrorAsync(externalToolDependencyException);

        return externalToolDependencyException;
    }

    private async ValueTask<ExternalToolServiceException> CreateAndLogServiceExceptionAsync(
        Xeption? exception)
    {
        var externalToolServiceException =
            new ExternalToolServiceException(
                message: "External tool service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(externalToolServiceException);

        return externalToolServiceException;
    }
}
