// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Coordinations.Agents.Exceptions;
using Standard.Agents.Models.Orchestrations.Agents.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Managements;

public partial class RunManagementService
{
    private delegate ValueTask<T> ReturningFunction<T>();

    private async ValueTask<T> TryCatch<T>(ReturningFunction<T> returningFunction)
    {
        try
        {
            return await returningFunction();
        }
        catch (Exception exception)
        {
            throw await MappedAndLoggedAsync(exception);
        }
    }

    // The mapping itself, callable without a surrounding try — which is what the streamed door
    // needs: an async iterator cannot wrap a yield in a catch, so it advances the enumeration
    // inside one and maps here. One copy, so the two doors cannot grow different families.
    private async ValueTask<Exception> MappedAndLoggedAsync(Exception exception)
    {
        switch (exception)
        {
            case InvalidAgentException invalidAgentException:
                return await CreateAndLogValidationExceptionAsync(invalidAgentException);

            case AgentOrchestrationValidationException agentOrchestrationValidationException:
                return await CreateAndLogDependencyValidationExceptionAsync(
                    agentOrchestrationValidationException.InnerException as Xeption);

            case AgentOrchestrationDependencyValidationException agentOrchestrationDependencyValidationException:
                return await CreateAndLogDependencyValidationExceptionAsync(
                    agentOrchestrationDependencyValidationException.InnerException as Xeption);

            case AgentOrchestrationDependencyException agentOrchestrationDependencyException:
                return await CreateAndLogDependencyExceptionAsync(
                    agentOrchestrationDependencyException.InnerException as Xeption);

            case AgentOrchestrationServiceException agentOrchestrationServiceException:
                return await CreateAndLogDependencyExceptionAsync(
                    agentOrchestrationServiceException.InnerException as Xeption);

            default:
                var failedRunManagementServiceException =
                    new FailedRunManagementServiceException(
                        message: "Failed agent coordination service error occurred, contact support.",
                        innerException: exception);

                return await CreateAndLogServiceExceptionAsync(
                    failedRunManagementServiceException);
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

    private async ValueTask<AgentCoordinationDependencyValidationException>
        CreateAndLogDependencyValidationExceptionAsync(
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

    private async ValueTask<RunManagementServiceException> CreateAndLogServiceExceptionAsync(
        Xeption? exception)
    {
        var agentCoordinationServiceException =
            new RunManagementServiceException(
                message: "Agent coordination service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(agentCoordinationServiceException);

        return agentCoordinationServiceException;
    }
}
