// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.ExternalTools.Exceptions;
using Standard.Agents.Models.Foundations.InternalTools.Exceptions;
using Standard.Agents.Models.Foundations.Returns.Exceptions;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Agents.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Orchestrations.Direction;

public partial class DirectionOrchestrationService
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
        catch (InternalToolValidationException internalToolValidationException)
        {
            throw await CreateAndLogDependencyValidationExceptionAsync(
                internalToolValidationException.InnerException as Xeption);
        }
        catch (ExternalToolValidationException externalToolValidationException)
        {
            throw await CreateAndLogDependencyValidationExceptionAsync(
                externalToolValidationException.InnerException as Xeption);
        }
        catch (ExternalToolDependencyValidationException externalToolDependencyValidationException)
        {
            throw await CreateAndLogDependencyValidationExceptionAsync(
                externalToolDependencyValidationException.InnerException as Xeption);
        }
        catch (ReturnValidationException returnValidationException)
        {
            throw await CreateAndLogDependencyValidationExceptionAsync(
                returnValidationException.InnerException as Xeption);
        }
        catch (InternalToolDependencyException internalToolDependencyException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                internalToolDependencyException.InnerException as Xeption);
        }
        catch (InternalToolServiceException internalToolServiceException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                internalToolServiceException.InnerException as Xeption);
        }
        catch (ExternalToolDependencyException externalToolDependencyException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                externalToolDependencyException.InnerException as Xeption);
        }
        catch (ExternalToolServiceException externalToolServiceException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                externalToolServiceException.InnerException as Xeption);
        }
        catch (ReturnServiceException returnServiceException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                returnServiceException.InnerException as Xeption);
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
