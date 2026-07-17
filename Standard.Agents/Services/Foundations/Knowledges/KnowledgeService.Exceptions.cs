// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Knowledges.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Foundations.Knowledges;

public partial class KnowledgeService
{
    private delegate ValueTask<IReadOnlyList<string>> ReturningDocumentsFunction();

    private async ValueTask<IReadOnlyList<string>> TryCatch(
        ReturningDocumentsFunction returningDocumentsFunction)
    {
        try
        {
            return await returningDocumentsFunction();
        }
        catch (InvalidKnowledgeException invalidKnowledgeException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidKnowledgeException);
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
            var failedKnowledgeServiceException =
                new FailedKnowledgeServiceException(
                    message: "Failed knowledge service error occurred, contact support.",
                    innerException: exception);

            throw await CreateAndLogServiceExceptionAsync(failedKnowledgeServiceException);
        }
    }

    private async ValueTask<KnowledgeValidationException> CreateAndLogValidationExceptionAsync(
        Xeption? exception)
    {
        var knowledgeValidationException =
            new KnowledgeValidationException(
                message: "Knowledge validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(knowledgeValidationException);

        return knowledgeValidationException;
    }

    private async ValueTask<KnowledgeDependencyException> CreateAndLogCriticalDependencyExceptionAsync(
        Exception exception)
    {
        var failedKnowledgeDependencyException =
            new FailedKnowledgeDependencyException(
                message: "Failed knowledge dependency error occurred, contact support.",
                innerException: exception);

        var knowledgeDependencyException =
            new KnowledgeDependencyException(
                message: "Knowledge dependency error occurred, contact support.",
                innerException: failedKnowledgeDependencyException);

        await this.loggingBroker.LogCriticalAsync(knowledgeDependencyException);

        return knowledgeDependencyException;
    }

    private async ValueTask<KnowledgeDependencyException> CreateAndLogDependencyExceptionAsync(
        Exception exception)
    {
        var failedKnowledgeDependencyException =
            new FailedKnowledgeDependencyException(
                message: "Failed knowledge dependency error occurred, contact support.",
                innerException: exception);

        var knowledgeDependencyException =
            new KnowledgeDependencyException(
                message: "Knowledge dependency error occurred, contact support.",
                innerException: failedKnowledgeDependencyException);

        await this.loggingBroker.LogErrorAsync(knowledgeDependencyException);

        return knowledgeDependencyException;
    }

    private async ValueTask<KnowledgeServiceException> CreateAndLogServiceExceptionAsync(
        Xeption? exception)
    {
        var knowledgeServiceException =
            new KnowledgeServiceException(
                message: "Knowledge service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(knowledgeServiceException);

        return knowledgeServiceException;
    }
    }
