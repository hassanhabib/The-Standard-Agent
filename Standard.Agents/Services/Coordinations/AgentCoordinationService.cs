// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Orchestrations.Data;
using Standard.Agents.Services.Orchestrations.Decision;
using Standard.Agents.Services.Orchestrations.Direction;

namespace Standard.Agents.Services.Coordinations;

public partial class AgentCoordinationService : IAgentCoordinationService
{
    private const int DefaultMaxTurns = 7;

    private const string RetriesExhaustedMessage =
        "I can't help with that at the moment.";

    private readonly IDataOrchestrationService dataOrchestrationService;
    private readonly IDecisionOrchestrationService decisionOrchestrationService;
    private readonly IDirectionOrchestrationService directionOrchestrationService;
    private readonly ILoggingBroker loggingBroker;
    private readonly int maxTurns;

    public AgentCoordinationService(
        IDataOrchestrationService dataOrchestrationService,
        IDecisionOrchestrationService decisionOrchestrationService,
        IDirectionOrchestrationService directionOrchestrationService,
        ILoggingBroker loggingBroker,
        int maxTurns = DefaultMaxTurns)
    {
        this.dataOrchestrationService = dataOrchestrationService;
        this.decisionOrchestrationService = decisionOrchestrationService;
        this.directionOrchestrationService = directionOrchestrationService;
        this.loggingBroker = loggingBroker;
        this.maxTurns = maxTurns;
    }

    public ValueTask<string> ProcessPromptAsync(string prompt) =>
    TryCatch(async () =>
    {
        ValidatePrompt(prompt);

        await this.loggingBroker.LogResetAsync();

        AgentContext context = new() { Prompt = prompt };

        for (int turn = 0; turn < this.maxTurns; turn++)
        {
            await this.loggingBroker.LogTurnAsync(turn);

            await this.loggingBroker.LogStepAsync(AgentStep.Data);
            context = await this.dataOrchestrationService.RecallAsync(context);

            await this.loggingBroker.LogStepAsync(AgentStep.Decision);
            context = await this.decisionOrchestrationService.ThinkAsync(context);

            if (context.Status is AgentStatus.Revising)
            {
                await this.loggingBroker.LogOutcomeAsync($"turn {turn}: revising");

                continue;
            }

            await this.loggingBroker.LogStepAsync(AgentStep.Direction);
            context = await this.directionOrchestrationService.ActAsync(context);

            await this.loggingBroker.LogOutcomeAsync($"turn {turn}: {context.Status}");

            if (context.Status != AgentStatus.Working)
            {
                break;
            }
        }

        if (context.Status is AgentStatus.Revising)
        {
            context = context with
            {
                Result = RetriesExhaustedMessage,
                Status = AgentStatus.Refused
            };
        }

        await this.loggingBroker.LogOutcomeAsync($"done: {context.Status}");

        if (string.IsNullOrEmpty(context.Remember) is false)
        {
            await this.dataOrchestrationService.RememberAsync(context.Remember);
        }

        return context.Result;
    });

    public async IAsyncEnumerable<AgentStreamEvent> ProcessPromptStreamAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidatePrompt(prompt);

        await this.loggingBroker.LogResetAsync();

        AgentContext context = new() { Prompt = prompt };

        for (int turn = 0; turn < this.maxTurns; turn++)
        {
            await this.loggingBroker.LogTurnAsync(turn);

            await this.loggingBroker.LogStepAsync(AgentStep.Data);
            context = await this.dataOrchestrationService.RecallAsync(context);

            await this.loggingBroker.LogStepAsync(AgentStep.Decision);

            IDecisionStream decisionStream =
                this.decisionOrchestrationService.ThinkStreamAsync(context, cancellationToken);

            await foreach (AgentStreamEvent decisionEvent in
                decisionStream.WithCancellation(cancellationToken))
            {
                yield return decisionEvent;
            }

            context = decisionStream.Result;

            if (context.Status is AgentStatus.Revising)
            {
                await this.loggingBroker.LogOutcomeAsync($"turn {turn}: revising");

                continue;
            }

            await this.loggingBroker.LogStepAsync(AgentStep.Direction);
            context = await this.directionOrchestrationService.ActAsync(context);

            await this.loggingBroker.LogOutcomeAsync($"turn {turn}: {context.Status}");

            if (context.Status is AgentStatus.Working
                && string.IsNullOrEmpty(context.Result) is false)
            {
                yield return new AgentStreamEvent(
                    AgentStreamEventType.Tool,
                    $"{context.DirectionType}: {context.Result}");
            }

            if (context.Status is AgentStatus.Responded
                or AgentStatus.Refused
                or AgentStatus.AwaitingInput
                && string.IsNullOrEmpty(context.Result) is false)
            {
                yield return new AgentStreamEvent(
                    AgentStreamEventType.Response,
                    context.Result);
            }

            if (context.Status is not AgentStatus.Working)
            {
                break;
            }
        }

        if (context.Status is AgentStatus.Revising)
        {
            yield return new AgentStreamEvent(
                AgentStreamEventType.Status,
                "unable to satisfy review after retries; refusing");

            yield return new AgentStreamEvent(
                AgentStreamEventType.Response,
                RetriesExhaustedMessage);
        }

        await this.loggingBroker.LogOutcomeAsync($"done: {context.Status}");

        if (string.IsNullOrEmpty(context.Remember) is false)
        {
            await this.dataOrchestrationService.RememberAsync(context.Remember);
        }
    }
}
