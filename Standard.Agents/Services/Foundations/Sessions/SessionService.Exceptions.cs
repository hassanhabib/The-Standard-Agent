// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Foundations.Sessions.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Foundations.Sessions;

public partial class SessionService
{
    private delegate ValueTask<AgentSession?> ReturningSessionFunction();
    private delegate ValueTask ReturningNothingFunction();

    private async ValueTask<AgentSession?> TryCatch(
        ReturningSessionFunction returningSessionFunction)
    {
        try
        {
            return await returningSessionFunction();
        }
        catch (InvalidSessionException invalidSessionException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidSessionException);
        }
        catch (FileNotFoundException fileNotFoundException)
        {
            throw await CreateAndLogCriticalDependencyExceptionAsync(fileNotFoundException);
        }
        catch (DirectoryNotFoundException directoryNotFoundException)
        {
            throw await CreateAndLogCriticalDependencyExceptionAsync(directoryNotFoundException);
        }
        catch (UnauthorizedAccessException unauthorizedAccessException)
        {
            throw await CreateAndLogCriticalDependencyExceptionAsync(unauthorizedAccessException);
        }
        catch (System.Data.DBConcurrencyException dbConcurrencyException)
        {
            var staleSessionException =
                new StaleSessionException(
                    message: "The session was written by another prompt since it was read; read it again and retry.",
                    innerException: dbConcurrencyException);

            throw await CreateAndLogDependencyValidationExceptionAsync(staleSessionException);
        }
        catch (IOException ioException)
        {
            throw await CreateAndLogDependencyExceptionAsync(ioException);
        }
        catch (Exception exception)
        {
            var failedSessionServiceException =
                new FailedSessionServiceException(
                    message: "Failed session service error occurred, contact support.",
                    innerException: exception);

            throw await CreateAndLogServiceExceptionAsync(failedSessionServiceException);
        }
    }

    private async ValueTask TryCatch(ReturningNothingFunction returningNothingFunction)
    {
        try
        {
            await returningNothingFunction();
        }
        catch (InvalidSessionException invalidSessionException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidSessionException);
        }
        catch (FileNotFoundException fileNotFoundException)
        {
            throw await CreateAndLogCriticalDependencyExceptionAsync(fileNotFoundException);
        }
        catch (DirectoryNotFoundException directoryNotFoundException)
        {
            throw await CreateAndLogCriticalDependencyExceptionAsync(directoryNotFoundException);
        }
        catch (UnauthorizedAccessException unauthorizedAccessException)
        {
            throw await CreateAndLogCriticalDependencyExceptionAsync(unauthorizedAccessException);
        }
        catch (System.Data.DBConcurrencyException dbConcurrencyException)
        {
            var staleSessionException =
                new StaleSessionException(
                    message: "The session was written by another prompt since it was read; read it again and retry.",
                    innerException: dbConcurrencyException);

            throw await CreateAndLogDependencyValidationExceptionAsync(staleSessionException);
        }
        catch (IOException ioException)
        {
            throw await CreateAndLogDependencyExceptionAsync(ioException);
        }
        catch (Exception exception)
        {
            var failedSessionServiceException =
                new FailedSessionServiceException(
                    message: "Failed session service error occurred, contact support.",
                    innerException: exception);

            throw await CreateAndLogServiceExceptionAsync(failedSessionServiceException);
        }
    }

    private async ValueTask<SessionValidationException> CreateAndLogValidationExceptionAsync(
        Xeption? exception)
    {
        var sessionValidationException =
            new SessionValidationException(
                message: "Session validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(sessionValidationException);

        return sessionValidationException;
    }

    private async ValueTask<SessionDependencyException>
        CreateAndLogCriticalDependencyExceptionAsync(Exception exception)
    {
        var failedSessionDependencyException =
            new FailedSessionDependencyException(
                message: "Failed session dependency error occurred, contact support.",
                innerException: exception);

        var sessionDependencyException =
            new SessionDependencyException(
                message: "Session dependency error occurred, contact support.",
                innerException: failedSessionDependencyException);

        await this.loggingBroker.LogCriticalAsync(sessionDependencyException);

        return sessionDependencyException;
    }

    private async ValueTask<SessionDependencyValidationException>
        CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
    {
        var sessionDependencyValidationException =
            new SessionDependencyValidationException(
                message: "Session dependency validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(sessionDependencyValidationException);

        return sessionDependencyValidationException;
    }

    private async ValueTask<SessionDependencyException> CreateAndLogDependencyExceptionAsync(
        Exception exception)
    {
        var failedSessionDependencyException =
            new FailedSessionDependencyException(
                message: "Failed session dependency error occurred, contact support.",
                innerException: exception);

        var sessionDependencyException =
            new SessionDependencyException(
                message: "Session dependency error occurred, contact support.",
                innerException: failedSessionDependencyException);

        await this.loggingBroker.LogErrorAsync(sessionDependencyException);

        return sessionDependencyException;
    }

    private async ValueTask<SessionServiceException> CreateAndLogServiceExceptionAsync(
        Xeption? exception)
    {
        var sessionServiceException =
            new SessionServiceException(
                message: "Session service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(sessionServiceException);

        return sessionServiceException;
    }
}
