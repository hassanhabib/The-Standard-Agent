// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Logs;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Orchestrations.Data;
using Standard.Agents.Services.Orchestrations.Decision;
using Standard.Agents.Services.Orchestrations.Direction;

namespace Standard.Agents.Services.Coordinations;

public partial class AgentCoordinationService : IAgentCoordinationService
{
    private const int MaxTurns = 7;

    private const string RetriesExhaustedMessage =
        "I can't help with that at the moment.";

    private readonly IDataOrchestrationService dataOrchestrationService;
    private readonly IDecisionOrchestrationService decisionOrchestrationService;
    private readonly IDirectionOrchestrationService directionOrchestrationService;
    private readonly ILogBroker logBroker;
    private readonly ILoggingBroker loggingBroker;

    public AgentCoordinationService(
        IDataOrchestrationService dataOrchestrationService,
        IDecisionOrchestrationService decisionOrchestrationService,
        IDirectionOrchestrationService directionOrchestrationService,
        ILogBroker logBroker,
        ILoggingBroker loggingBroker)
    {
        this.dataOrchestrationService = dataOrchestrationService;
        this.decisionOrchestrationService = decisionOrchestrationService;
        this.directionOrchestrationService = directionOrchestrationService;
        this.logBroker = logBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<string> ProcessPromptAsync(string prompt) =>
    TryCatch(async () =>
    {
        ValidatePrompt(prompt);

        await this.logBroker.ResetAsync();

        AgentContext context = new() { Prompt = prompt };

        for (int turn = 1; turn <= MaxTurns; turn++)
        {
            context = await this.dataOrchestrationService.RecallAsync(context);
            context = await this.decisionOrchestrationService.ThinkAsync(context);

            if (context.Status is AgentStatus.Revising)
            {
                await LogTurnAsync(turn, context);

                continue;
            }

            context = await this.directionOrchestrationService.ActAsync(context);

            await LogTurnAsync(turn, context);

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

        return context.Result;
    });

    public async IAsyncEnumerable<AgentStreamEvent> ProcessPromptStreamAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidatePrompt(prompt);

        await this.logBroker.ResetAsync();

        AgentContext context = new() { Prompt = prompt };

        for (int turn = 1; turn <= MaxTurns; turn++)
        {
            context = await this.dataOrchestrationService.RecallAsync(context);

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
                await LogTurnAsync(turn, context);

                continue;
            }

            context = await this.directionOrchestrationService.ActAsync(context);

            if (context.Status is AgentStatus.Working
                && string.IsNullOrEmpty(context.Result) is false)
            {
                yield return new AgentStreamEvent(
                    AgentStreamEventType.Tool,
                    $"{context.DirectionType}: {context.Result}");
            }

            await LogTurnAsync(turn, context);

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
    }

    private async ValueTask LogTurnAsync(int turn, AgentContext context) =>
        await this.logBroker.WriteAsync(
            $"turn {turn} | intent: {context.Intent} | direction: {context.DirectionType} " +
            $"| status: {context.Status}");
}
