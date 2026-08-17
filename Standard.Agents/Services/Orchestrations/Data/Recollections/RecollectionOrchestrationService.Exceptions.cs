// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Knowledges.Exceptions;
using Standard.Agents.Models.Foundations.Memorys.Exceptions;
using Standard.Agents.Models.Foundations.Sessions.Exceptions;
using Standard.Agents.Models.Foundations.Skills.Exceptions;
using Standard.Agents.Models.Orchestrations.Agents.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Orchestrations.Data.Recollections;

// A region localizes its foundations' failures into the orchestration family, so the tier above
// never has to know which store was behind the nature it asked for.
public partial class RecollectionOrchestrationService
{
    private async ValueTask<T> TryCatch<T>(Func<ValueTask<T>> returningFunction)
    {
        try
        {
            return await returningFunction();
        }
        catch (Exception exception) when (exception is MemoryValidationException
            or KnowledgeValidationException
            or SessionValidationException)
        {
            throw await CreateAndLogDependencyValidationExceptionAsync(
                (exception as Xeption)?.InnerException as Xeption);
        }
        catch (Exception exception) when (exception is SkillDependencyException
            or SkillServiceException
            or MemoryDependencyException
            or MemoryServiceException
            or KnowledgeDependencyException
            or KnowledgeServiceException
            or SessionDependencyException
            or SessionServiceException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                (exception as Xeption)?.InnerException as Xeption);
        }
        catch (Exception exception)
        {
            var failedException =
                new FailedAgentOrchestrationServiceException(
                    message: "Failed agent orchestration service error occurred, contact support.",
                    innerException: exception);

            throw await CreateAndLogServiceExceptionAsync(failedException);
        }
    }

    private async ValueTask<AgentOrchestrationDependencyValidationException>
        CreateAndLogDependencyValidationExceptionAsync(Xeption? exception)
    {
        var dependencyValidationException =
            new AgentOrchestrationDependencyValidationException(
                message: "Agent orchestration dependency validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(dependencyValidationException);

        return dependencyValidationException;
    }

    private async ValueTask<AgentOrchestrationDependencyException>
        CreateAndLogDependencyExceptionAsync(Xeption? exception)
    {
        var dependencyException =
            new AgentOrchestrationDependencyException(
                message: "Agent orchestration dependency error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(dependencyException);

        return dependencyException;
    }

    private async ValueTask<AgentOrchestrationServiceException>
        CreateAndLogServiceExceptionAsync(Xeption? exception)
    {
        var serviceException =
            new AgentOrchestrationServiceException(
                message: "Agent orchestration service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(serviceException);

        return serviceException;
    }
}
