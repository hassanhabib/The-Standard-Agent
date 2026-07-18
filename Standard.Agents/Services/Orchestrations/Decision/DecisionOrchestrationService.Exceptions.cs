// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Brains.Exceptions;
using Standard.Agents.Models.Foundations.Gates.Exceptions;
using Standard.Agents.Models.Foundations.Judges.Exceptions;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Agents.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Orchestrations.Decision;

public partial class DecisionOrchestrationService
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
        catch (GateValidationException gateValidationException)
        {
            throw await CreateAndLogDependencyValidationExceptionAsync(
                gateValidationException.InnerException as Xeption);
        }
        catch (GateDependencyValidationException gateDependencyValidationException)
        {
            throw await CreateAndLogDependencyValidationExceptionAsync(
                gateDependencyValidationException.InnerException as Xeption);
        }
        catch (BrainValidationException brainValidationException)
        {
            throw await CreateAndLogDependencyValidationExceptionAsync(
                brainValidationException.InnerException as Xeption);
        }
        catch (BrainDependencyValidationException brainDependencyValidationException)
        {
            throw await CreateAndLogDependencyValidationExceptionAsync(
                brainDependencyValidationException.InnerException as Xeption);
        }
        catch (JudgeValidationException judgeValidationException)
        {
            throw await CreateAndLogDependencyValidationExceptionAsync(
                judgeValidationException.InnerException as Xeption);
        }
        catch (JudgeDependencyValidationException judgeDependencyValidationException)
        {
            throw await CreateAndLogDependencyValidationExceptionAsync(
                judgeDependencyValidationException.InnerException as Xeption);
        }
        catch (GateDependencyException gateDependencyException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                gateDependencyException.InnerException as Xeption);
        }
        catch (GateServiceException gateServiceException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                gateServiceException.InnerException as Xeption);
        }
        catch (BrainDependencyException brainDependencyException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                brainDependencyException.InnerException as Xeption);
        }
        catch (BrainServiceException brainServiceException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                brainServiceException.InnerException as Xeption);
        }
        catch (JudgeDependencyException judgeDependencyException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                judgeDependencyException.InnerException as Xeption);
        }
        catch (JudgeServiceException judgeServiceException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                judgeServiceException.InnerException as Xeption);
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

    private async ValueTask<AgentOrchestrationDependencyValidationException>
        CreateAndLogDependencyValidationExceptionAsync(
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
