// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Knowledges.Exceptions;
using Standard.Agents.Models.Foundations.Memorys.Exceptions;
using Standard.Agents.Models.Foundations.Skills.Exceptions;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Agents.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Orchestrations.Data;

public partial class DataOrchestrationService
{
    private delegate ValueTask<AgentContext> ReturningContextFunction();

    private async ValueTask<AgentContext> TryCatch(
ReturningContextFunction returningContextFunction)
    {
        try
        {
            return await returningContextFunction();
        }
        catch (NullAgentContextException nullAgentContextException)
        {
            throw await CreateAndLogValidationExceptionAsync(nullAgentContextException);
        }
        catch (MemoryValidationException memoryValidationException)
        {
            throw await CreateAndLogDependencyValidationExceptionAsync(
                memoryValidationException.InnerException as Xeption);
        }
        catch (KnowledgeValidationException knowledgeValidationException)
        {
            throw await CreateAndLogDependencyValidationExceptionAsync(
                knowledgeValidationException.InnerException as Xeption);
        }
        catch (SkillDependencyException skillDependencyException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                skillDependencyException.InnerException as Xeption);
        }
        catch (SkillServiceException skillServiceException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                skillServiceException.InnerException as Xeption);
        }
        catch (MemoryDependencyException memoryDependencyException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                memoryDependencyException.InnerException as Xeption);
        }
        catch (MemoryServiceException memoryServiceException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                memoryServiceException.InnerException as Xeption);
        }
        catch (KnowledgeDependencyException knowledgeDependencyException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                knowledgeDependencyException.InnerException as Xeption);
        }
        catch (KnowledgeServiceException knowledgeServiceException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                knowledgeServiceException.InnerException as Xeption);
        }
        catch (Exception exception)
        {
            var failedAgentOrchestrationServiceException =
                new FailedAgentOrchestrationServiceException(
                    message: "Failed agent orchestration service error occurred, contact support.",
                    innerException: exception);

            throw await CreateAndLogServiceExceptionAsync(
                failedAgentOrchestrationServiceException);
        }
    }

    private async ValueTask<AgentOrchestrationValidationException> CreateAndLogValidationExceptionAsync(
        Xeption? exception)
    {
        var agentOrchestrationValidationException =
            new AgentOrchestrationValidationException(
                message: "Agent orchestration validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(agentOrchestrationValidationException);

        return agentOrchestrationValidationException;
    }

    private async ValueTask<AgentOrchestrationDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
        Xeption? exception)
    {
        var agentOrchestrationDependencyValidationException =
            new AgentOrchestrationDependencyValidationException(
                message: "Agent orchestration dependency validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(agentOrchestrationDependencyValidationException);

        return agentOrchestrationDependencyValidationException;
    }

    private async ValueTask<AgentOrchestrationDependencyException> CreateAndLogDependencyExceptionAsync(
        Xeption? exception)
    {
        var agentOrchestrationDependencyException =
            new AgentOrchestrationDependencyException(
                message: "Agent orchestration dependency error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(agentOrchestrationDependencyException);

        return agentOrchestrationDependencyException;
    }

    private async ValueTask<AgentOrchestrationServiceException> CreateAndLogServiceExceptionAsync(
        Xeption? exception)
    {
        var agentOrchestrationServiceException =
            new AgentOrchestrationServiceException(
                message: "Agent orchestration service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(agentOrchestrationServiceException);

        return agentOrchestrationServiceException;
    }
}
