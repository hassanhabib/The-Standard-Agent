// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Orchestrations.Data;
using Standard.Agents.Services.Orchestrations.Decision;
using Standard.Agents.Services.Orchestrations.Direction;

namespace Standard.Agents.Services.Coordinations;

public partial class AgentCoordinationService : IAgentCoordinationService
{
                        private const int MaxTurns = 7;

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
            context = await this.directionOrchestrationService.ActAsync(context);

            await LogTurnAsync(turn, context);

                                                if (context.Status != AgentStatus.Working)
            {
                break;
            }
        }

        return context.Result;
    });

    private async ValueTask LogTurnAsync(int turn, AgentContext context) =>
        await this.logBroker.WriteAsync(
            $"turn {turn} | intent: {context.Intent} | direction: {context.DirectionType} " +
            $"| status: {context.Status}");
}
