// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Effects;
using Standard.Agents.Services.Foundations.Gates;
using Standard.Agents.Services.Orchestrations.Direction.Executions;
using Standard.Agents.Services.Orchestrations.Direction.Perimeters;

namespace Standard.Agents.Services.Coordinations.Direction;

// The Direction nature: two regions, and the ORDER between them.
//
// Perimeter answers whether an act may happen; Execution performs it. Neither can own the
// sequence, because the sequence interleaves them — authorize, record the intent, approve,
// execute, record the outcome. That interleaving is Direction's own logic, which is why it lives
// in Direction's own service rather than in a generic sequencing tier.
public partial class DirectionCoordinationService : IDirectionCoordinationService
{
    private const string ReturnResponseDirection = "ReturnResponse";
    private const string RefuseDirection = "Refuse";
    private const string AwaitInputDirection = "AwaitInput";

    private readonly IPerimeterOrchestrationService perimeterService;
    private readonly IExecutionOrchestrationService executionService;
    private readonly ILoggingBroker loggingBroker;
    private readonly IGateService? screeningService;
    private readonly HashSet<string> irreversibleToolNames;

    // Asked once per act rather than captured once at composition, because who is acting can
    // change between prompts on the same agent — a singleton serving many callers is the shape
    // this framework asks hosts to adopt (SPEC.md §4.4).
    private readonly Func<AgentPrincipal?>? identityResolver;

    public DirectionCoordinationService(
        IPerimeterOrchestrationService perimeterService,
        IExecutionOrchestrationService executionService,
        ILoggingBroker loggingBroker,
        IEnumerable<string>? irreversibleTools = null,
        IGateService? screeningService = null,
        Func<AgentPrincipal?>? identityResolver = null)
    {
        this.perimeterService = perimeterService;
        this.executionService = executionService;
        this.loggingBroker = loggingBroker;
        this.screeningService = screeningService;
        this.identityResolver = identityResolver;

        this.irreversibleToolNames =
            new HashSet<string>(irreversibleTools ?? [], StringComparer.OrdinalIgnoreCase);
    }

    public ValueTask<AgentContext> ActAsync(AgentContext context) =>
    TryCatch(async () =>
    {
        ValidateContext(context);

        if (IsTerminal(context.DirectionType))
        {
            string result = await this.executionService.ReturnAsync(context.Payload);

            await this.loggingBroker.LogProcessAsync(
                "Direction", $"{context.DirectionType} → returned: {result}");

            return context with
            {
                Result = result,
                Status = ToTerminalStatus(context.DirectionType)
            };
        }

        return await ActOnEffectAsync(context);
    });

    private static bool IsTerminal(string directionType) =>
        directionType.Equals(ReturnResponseDirection, StringComparison.OrdinalIgnoreCase)
            || directionType.Equals(RefuseDirection, StringComparison.OrdinalIgnoreCase)
            || directionType.Equals(AwaitInputDirection, StringComparison.OrdinalIgnoreCase);

    private static AgentStatus ToTerminalStatus(string directionType)
    {
        if (directionType.Equals(RefuseDirection, StringComparison.OrdinalIgnoreCase))
        {
            return AgentStatus.Refused;
        }

        if (directionType.Equals(AwaitInputDirection, StringComparison.OrdinalIgnoreCase))
        {
            return AgentStatus.AwaitingInput;
        }

        return AgentStatus.Responded;
    }
}
