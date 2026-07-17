// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using MinimalAgent.Models;
using MinimalAgent.Services.Orchestrations.Data;
using MinimalAgent.Services.Orchestrations.Decision;
using MinimalAgent.Services.Orchestrations.Direction;

namespace MinimalAgent.Services.Coordinations;

public sealed class AgentCoordinationService : IAgentCoordinationService
{
    private const int MaxTurns = 7;

    private readonly IDataOrchestrationService dataOrchestration;
    private readonly IDecisionOrchestrationService decisionOrchestration;
    private readonly IDirectionOrchestrationService directionOrchestration;

    public AgentCoordinationService(
        IDataOrchestrationService dataOrchestration,
        IDecisionOrchestrationService decisionOrchestration,
        IDirectionOrchestrationService directionOrchestration)
    {
        this.dataOrchestration = dataOrchestration;
        this.decisionOrchestration = decisionOrchestration;
        this.directionOrchestration = directionOrchestration;
    }

    public async ValueTask<string> ProcessPromptAsync(string prompt)
    {
        AgentContext context = new() { Prompt = prompt };

        for (int turn = 1; turn <= MaxTurns; turn++)
        {
            context = await this.dataOrchestration.RecallAsync(context);
            context = await this.decisionOrchestration.ThinkAsync(context);
            context = await this.directionOrchestration.ActAsync(context);

            if (context.Status != AgentStatus.Working)
            {
                break;
            }
        }

        return context.Result;
    }
}
