// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Approvals.Exceptions;
using Standard.Agents.Models.Foundations.EffectLedgers.Exceptions;
using Standard.Agents.Models.Foundations.ExternalTools.Exceptions;
using Standard.Agents.Models.Foundations.InternalTools.Exceptions;
using Standard.Agents.Models.Foundations.Policys.Exceptions;
using Standard.Agents.Models.Foundations.Returns.Exceptions;
using Standard.Agents.Models.Orchestrations.Agents.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Orchestrations.Direction.Executions;

public partial class ExecutionOrchestrationService
{
    private async ValueTask<T> TryCatch<T>(Func<ValueTask<T>> returningFunction)
    {
        try
        {
            return await returningFunction();
        }
        catch (Exception exception) when (exception is PolicyValidationException
            or ApprovalValidationException
            or EffectLedgerValidationException
            or InternalToolValidationException
            or ExternalToolValidationException
            or ExternalToolDependencyValidationException
            or ReturnValidationException)
        {
            throw await CreateAndLogDependencyValidationExceptionAsync(
                (exception as Xeption)?.InnerException as Xeption);
        }
        catch (Exception exception) when (exception is PolicyDependencyException
            or PolicyServiceException
            or ApprovalDependencyException
            or ApprovalServiceException
            or EffectLedgerDependencyException
            or EffectLedgerServiceException
            or InternalToolDependencyException
            or InternalToolServiceException
            or ExternalToolDependencyException
            or ExternalToolServiceException
            or ReturnServiceException)
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
