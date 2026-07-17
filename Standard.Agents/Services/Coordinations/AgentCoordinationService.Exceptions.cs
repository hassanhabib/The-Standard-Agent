// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Coordinations.Agents.Exceptions;
using Standard.Agents.Models.Orchestrations.Agents.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Coordinations;

public partial class AgentCoordinationService
{
    private delegate ValueTask<string> ReturningStringFunction();

    // The last tier. Per the decision on #34, this is where a failed turn surfaces:
    // nothing sets AgentStatus.Failed, so an unrecoverable failure arrives here as a
    // categorical exception and leaves as AgentCoordinationServiceException — the
    // caller's single, typed contract for "the agent could not answer".
    private async ValueTask<string> TryCatch(ReturningStringFunction returningStringFunction)
    {
        try
        {
            return await returningStringFunction();
        }
        catch (InvalidAgentException invalidAgentException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidAgentException);
        }
        catch (AgentOrchestrationValidationException agentOrchestrationValidationException)
        {
            throw await CreateAndLogDependencyValidationExceptionAsync(
                agentOrchestrationValidationException.InnerException as Xeption);
        }
        catch (AgentOrchestrationDependencyValidationException agentOrchestrationDependencyValidationException)
        {
            throw await CreateAndLogDependencyValidationExceptionAsync(
                agentOrchestrationDependencyValidationException.InnerException as Xeption);
        }
        catch (AgentOrchestrationDependencyException agentOrchestrationDependencyException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                agentOrchestrationDependencyException.InnerException as Xeption);
        }
        catch (AgentOrchestrationServiceException agentOrchestrationServiceException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                agentOrchestrationServiceException.InnerException as Xeption);
        }
        catch (Exception exception)
        {
            var failedAgentCoordinationServiceException =
                new FailedAgentCoordinationServiceException(
                    message: "Failed agent coordination service error occurred, contact support.",
                    innerException: exception);

            throw await CreateAndLogServiceExceptionAsync(
                failedAgentCoordinationServiceException);
        }
    }

    private async ValueTask<AgentCoordinationValidationException> CreateAndLogValidationExceptionAsync(
        Xeption? exception)
    {
        var agentCoordinationValidationException =
            new AgentCoordinationValidationException(
                message: "Agent coordination validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(agentCoordinationValidationException);

        return agentCoordinationValidationException;
    }

    private async ValueTask<AgentCoordinationDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
        Xeption? exception)
    {
        var agentCoordinationDependencyValidationException =
            new AgentCoordinationDependencyValidationException(
                message: "Agent coordination dependency validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(agentCoordinationDependencyValidationException);

        return agentCoordinationDependencyValidationException;
    }

    private async ValueTask<AgentCoordinationDependencyException> CreateAndLogDependencyExceptionAsync(
        Xeption? exception)
    {
        var agentCoordinationDependencyException =
            new AgentCoordinationDependencyException(
                message: "Agent coordination dependency error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(agentCoordinationDependencyException);

        return agentCoordinationDependencyException;
    }

    private async ValueTask<AgentCoordinationServiceException> CreateAndLogServiceExceptionAsync(
        Xeption? exception)
    {
        var agentCoordinationServiceException =
            new AgentCoordinationServiceException(
                message: "Agent coordination service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(agentCoordinationServiceException);

        return agentCoordinationServiceException;
    }
}
