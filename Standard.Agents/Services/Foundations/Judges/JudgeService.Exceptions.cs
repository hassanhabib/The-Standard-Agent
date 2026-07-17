// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using RESTFulSense.Exceptions;
using Standard.Agents.Models.Foundations.Judges.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Foundations.Judges;

public partial class JudgeService
{
    private delegate ValueTask<double> ReturningScoreFunction();

                private async ValueTask<double> TryCatch(ReturningScoreFunction returningScoreFunction)
    {
        try
        {
            return await returningScoreFunction();
        }
        catch (InvalidJudgeException invalidJudgeException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidJudgeException);
        }
        catch (InvalidJudgeScoreException invalidJudgeScoreException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidJudgeScoreException);
        }
        catch (HttpResponseBadRequestException httpResponseBadRequestException)
        {
            var invalidJudgeException =
                new InvalidJudgeException(
                    message: "Invalid judge request. Please correct the error and try again.");

            invalidJudgeException.AddData(httpResponseBadRequestException.Data);

            throw await CreateAndLogDependencyValidationExceptionAsync(invalidJudgeException);
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
            var failedJudgeServiceException =
                new FailedJudgeServiceException(
                    message: "Failed judge service error occurred, contact support.",
                    innerException: exception);

            throw await CreateAndLogServiceExceptionAsync(failedJudgeServiceException);
        }
    }

    private async ValueTask<JudgeValidationException> CreateAndLogValidationExceptionAsync(
        Xeption? exception)
    {
        var gateValidationException =
            new JudgeValidationException(
                message: "Judge validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(gateValidationException);

        return gateValidationException;
    }

    private async ValueTask<JudgeDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
        Xeption? exception)
    {
        var gateDependencyValidationException =
            new JudgeDependencyValidationException(
                message: "Judge dependency validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(gateDependencyValidationException);

        return gateDependencyValidationException;
    }

    private async ValueTask<JudgeDependencyException> CreateAndLogCriticalDependencyExceptionAsync(
        Exception exception)
    {
        var failedJudgeDependencyException =
            new FailedJudgeDependencyException(
                message: "Failed judge dependency error occurred, contact support.",
                innerException: exception);

        var gateDependencyException =
            new JudgeDependencyException(
                message: "Judge dependency error occurred, contact support.",
                innerException: failedJudgeDependencyException);

        await this.loggingBroker.LogCriticalAsync(gateDependencyException);

        return gateDependencyException;
    }

    private async ValueTask<JudgeDependencyException> CreateAndLogDependencyExceptionAsync(
        Exception exception)
    {
        var failedJudgeDependencyException =
            new FailedJudgeDependencyException(
                message: "Failed judge dependency error occurred, contact support.",
                innerException: exception);

        var gateDependencyException =
            new JudgeDependencyException(
                message: "Judge dependency error occurred, contact support.",
                innerException: failedJudgeDependencyException);

        await this.loggingBroker.LogErrorAsync(gateDependencyException);

        return gateDependencyException;
    }

    private async ValueTask<JudgeServiceException> CreateAndLogServiceExceptionAsync(
        Xeption? exception)
    {
        var gateServiceException =
            new JudgeServiceException(
                message: "Judge service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(gateServiceException);

        return gateServiceException;
    }
}
