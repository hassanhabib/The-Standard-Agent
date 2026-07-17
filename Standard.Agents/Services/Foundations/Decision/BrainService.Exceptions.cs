// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using RESTFulSense.Exceptions;
using Standard.Agents.Models.Foundations.Brains.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Foundations.Decision;

public partial class BrainService
{
    private delegate ValueTask<string> ReturningStringFunction();

    private async ValueTask<string> TryCatch(ReturningStringFunction returningStringFunction)
    {
        try
        {
            return await returningStringFunction();
        }
        catch (InvalidBrainException invalidBrainException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidBrainException);
        }
        catch (HttpResponseBadRequestException httpResponseBadRequestException)
        {
            // A 400 means we sent something wrong — our fault, not the endpoint's.
            var invalidBrainException =
                new InvalidBrainException(
                    message: "Invalid brain request. Please correct the error and try again.");

            invalidBrainException.AddData(httpResponseBadRequestException.Data);

            throw await CreateAndLogDependencyValidationExceptionAsync(invalidBrainException);
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
            var failedBrainServiceException =
                new FailedBrainServiceException(
                    message: "Failed brain service error occurred, contact support.",
                    innerException: exception);

            throw await CreateAndLogServiceExceptionAsync(failedBrainServiceException);
        }
    }

    private async ValueTask<BrainValidationException> CreateAndLogValidationExceptionAsync(
        Xeption exception)
    {
        var brainValidationException =
            new BrainValidationException(
                message: "Brain validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(brainValidationException);

        return brainValidationException;
    }

    private async ValueTask<BrainDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
        Xeption exception)
    {
        var brainDependencyValidationException =
            new BrainDependencyValidationException(
                message: "Brain dependency validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(brainDependencyValidationException);

        return brainDependencyValidationException;
    }

    private async ValueTask<BrainDependencyException> CreateAndLogCriticalDependencyExceptionAsync(
        Exception exception)
    {
        var failedBrainDependencyException =
            new FailedBrainDependencyException(
                message: "Failed brain dependency error occurred, contact support.",
                innerException: exception);

        var brainDependencyException =
            new BrainDependencyException(
                message: "Brain dependency error occurred, contact support.",
                innerException: failedBrainDependencyException);

        await this.loggingBroker.LogCriticalAsync(brainDependencyException);

        return brainDependencyException;
    }

    private async ValueTask<BrainDependencyException> CreateAndLogDependencyExceptionAsync(
        Exception exception)
    {
        var failedBrainDependencyException =
            new FailedBrainDependencyException(
                message: "Failed brain dependency error occurred, contact support.",
                innerException: exception);

        var brainDependencyException =
            new BrainDependencyException(
                message: "Brain dependency error occurred, contact support.",
                innerException: failedBrainDependencyException);

        await this.loggingBroker.LogErrorAsync(brainDependencyException);

        return brainDependencyException;
    }

    private async ValueTask<BrainServiceException> CreateAndLogServiceExceptionAsync(
        Xeption exception)
    {
        var brainServiceException =
            new BrainServiceException(
                message: "Brain service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(brainServiceException);

        return brainServiceException;
    }
}
