// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using RESTFulSense.Exceptions;
using Standard.Agents.Models.Foundations.Gates.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Foundations.Decision;

public partial class GateService
{
    private delegate ValueTask<string> ReturningStringFunction();

    private async ValueTask<string> TryCatch(ReturningStringFunction returningStringFunction)
    {
        try
        {
            return await returningStringFunction();
        }
        catch (InvalidGateException invalidGateException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidGateException);
        }
        catch (HttpResponseBadRequestException httpResponseBadRequestException)
        {
            var invalidGateException =
                new InvalidGateException(
                    message: "Invalid gate request. Please correct the error and try again.");

            invalidGateException.AddData(httpResponseBadRequestException.Data);

            throw await CreateAndLogDependencyValidationExceptionAsync(invalidGateException);
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
            var failedGateServiceException =
                new FailedGateServiceException(
                    message: "Failed gate service error occurred, contact support.",
                    innerException: exception);

            throw await CreateAndLogServiceExceptionAsync(failedGateServiceException);
        }
    }

    private async ValueTask<GateValidationException> CreateAndLogValidationExceptionAsync(
        Xeption? exception)
    {
        var gateValidationException =
            new GateValidationException(
                message: "Gate validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(gateValidationException);

        return gateValidationException;
    }

    private async ValueTask<GateDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
        Xeption? exception)
    {
        var gateDependencyValidationException =
            new GateDependencyValidationException(
                message: "Gate dependency validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(gateDependencyValidationException);

        return gateDependencyValidationException;
    }

    private async ValueTask<GateDependencyException> CreateAndLogCriticalDependencyExceptionAsync(
        Exception exception)
    {
        var failedGateDependencyException =
            new FailedGateDependencyException(
                message: "Failed gate dependency error occurred, contact support.",
                innerException: exception);

        var gateDependencyException =
            new GateDependencyException(
                message: "Gate dependency error occurred, contact support.",
                innerException: failedGateDependencyException);

        await this.loggingBroker.LogCriticalAsync(gateDependencyException);

        return gateDependencyException;
    }

    private async ValueTask<GateDependencyException> CreateAndLogDependencyExceptionAsync(
        Exception exception)
    {
        var failedGateDependencyException =
            new FailedGateDependencyException(
                message: "Failed gate dependency error occurred, contact support.",
                innerException: exception);

        var gateDependencyException =
            new GateDependencyException(
                message: "Gate dependency error occurred, contact support.",
                innerException: failedGateDependencyException);

        await this.loggingBroker.LogErrorAsync(gateDependencyException);

        return gateDependencyException;
    }

    private async ValueTask<GateServiceException> CreateAndLogServiceExceptionAsync(
        Xeption? exception)
    {
        var gateServiceException =
            new GateServiceException(
                message: "Gate service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(gateServiceException);

        return gateServiceException;
    }
}
