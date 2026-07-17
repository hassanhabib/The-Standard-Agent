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

// The Agent — and the only loop in the system.
//
// SPEC.md 4.4: Coordination MUST hold no nature logic beyond sequencing and
// observing. Everything below is one of those two things; the moment a decision
// appears here, it belongs in a nature instead.
public partial class AgentCoordinationService : IAgentCoordinationService
{
    // SPEC.md 5: MUST be finite, SHOULD be small. The number is ours.
    //
    // Finite is the load-bearing half. A Brain that never emits a terminal reply is
    // not hypothetical — it is vector 06 — and without a bound the agent does not
    // fail, it hangs, burning an inference call per turn forever.
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

        // The context is a local, born here and dying here. Invariant 7.4 — the
        // instance is ephemeral and stateless across prompts; holding this in a field
        // would leak one prompt's observations into the next.
        AgentContext context = new() { Prompt = prompt };

        for (int turn = 1; turn <= MaxTurns; turn++)
        {
            context = await this.dataOrchestrationService.RecallAsync(context);
            context = await this.decisionOrchestrationService.ThinkAsync(context);
            context = await this.directionOrchestrationService.ActAsync(context);

            await LogTurnAsync(turn, context);

            // Stops on ANY non-Working status rather than on a list of known ones, so
            // a new terminal state needs no change here. SPEC.md 3 forbids a boolean
            // for exactly this reason.
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
