// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Memorys.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Foundations.Memorys;

public partial class MemoryService
{
    private delegate ValueTask<IReadOnlyList<string>> ReturningMemoriesFunction();
    private delegate ValueTask ReturningNothingFunction();

    private async ValueTask<IReadOnlyList<string>> TryCatch(
        ReturningMemoriesFunction returningMemoriesFunction)
    {
        try
        {
            return await returningMemoriesFunction();
        }
        catch (InvalidMemoryException invalidMemoryException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidMemoryException);
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
        catch (IOException ioException)
        {
            throw await CreateAndLogDependencyExceptionAsync(ioException);
        }
        catch (Exception exception)
        {
            var failedMemoryServiceException =
                new FailedMemoryServiceException(
                    message: "Failed memory service error occurred, contact support.",
                    innerException: exception);

            throw await CreateAndLogServiceExceptionAsync(failedMemoryServiceException);
        }
    }

    private async ValueTask TryCatch(ReturningNothingFunction returningNothingFunction)
    {
        try
        {
            await returningNothingFunction();
        }
        catch (InvalidMemoryException invalidMemoryException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidMemoryException);
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
        catch (IOException ioException)
        {
            throw await CreateAndLogDependencyExceptionAsync(ioException);
        }
        catch (Exception exception)
        {
            var failedMemoryServiceException =
                new FailedMemoryServiceException(
                    message: "Failed memory service error occurred, contact support.",
                    innerException: exception);

            throw await CreateAndLogServiceExceptionAsync(failedMemoryServiceException);
        }
    }

    private async ValueTask<MemoryValidationException> CreateAndLogValidationExceptionAsync(
        Xeption? exception)
    {
        var memoryValidationException =
            new MemoryValidationException(
                message: "Memory validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(memoryValidationException);

        return memoryValidationException;
    }

    private async ValueTask<MemoryDependencyException> CreateAndLogCriticalDependencyExceptionAsync(
        Exception exception)
    {
        var failedMemoryDependencyException =
            new FailedMemoryDependencyException(
                message: "Failed memory dependency error occurred, contact support.",
                innerException: exception);

        var memoryDependencyException =
            new MemoryDependencyException(
                message: "Memory dependency error occurred, contact support.",
                innerException: failedMemoryDependencyException);

        await this.loggingBroker.LogCriticalAsync(memoryDependencyException);

        return memoryDependencyException;
    }

    private async ValueTask<MemoryDependencyException> CreateAndLogDependencyExceptionAsync(
        Exception exception)
    {
        var failedMemoryDependencyException =
            new FailedMemoryDependencyException(
                message: "Failed memory dependency error occurred, contact support.",
                innerException: exception);

        var memoryDependencyException =
            new MemoryDependencyException(
                message: "Memory dependency error occurred, contact support.",
                innerException: failedMemoryDependencyException);

        await this.loggingBroker.LogErrorAsync(memoryDependencyException);

        return memoryDependencyException;
    }

    private async ValueTask<MemoryServiceException> CreateAndLogServiceExceptionAsync(
        Xeption? exception)
    {
        var memoryServiceException =
            new MemoryServiceException(
                message: "Memory service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(memoryServiceException);

        return memoryServiceException;
    }
}
