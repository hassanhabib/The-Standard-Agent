// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Coordinations.Directions;
using Standard.Agents.Models.Loggings;
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
    private readonly HashSet<string> irreversibleToolNames;
    private readonly PermissionMode permissionMode;
    private readonly IReadOnlyDictionary<string, RiskLevel> declaredRisk;
    private readonly IReadOnlyDictionary<string, RiskLevel> toolRisk;
    private readonly IReadOnlyDictionary<string, Func<string, string>> toolScope;
    private readonly Func<AgentEffect, bool>? explicitlyPermits;
    private readonly bool enforceSelection;
    private readonly HashSet<string> advertisedToolNames;

    // Asked once per act rather than captured once at composition, because who is acting can
    // change between prompts on the same agent — a singleton serving many callers is the shape
    // this framework asks hosts to adopt (SPEC.md §4.4).
    private readonly Func<AgentPrincipal?>? identityResolver;

    // Two services, one utility broker, and the perimeter's standing orders as one datum.
    // The orders used to arrive as eight constructor parameters, three of them delegates that
    // participate in authorization — collaborators no dependency count could see. Policy is
    // Data: they travel in PerimeterPolicy, where adding one is a reviewed model diff.
    public DirectionCoordinationService(
        IPerimeterOrchestrationService perimeterService,
        IExecutionOrchestrationService executionService,
        ILoggingBroker loggingBroker,
        PerimeterPolicy? policy = null)
    {
        PerimeterPolicy standingOrders = policy ?? new PerimeterPolicy();

        this.permissionMode = standingOrders.Mode;
        this.declaredRisk = standingOrders.DeclaredRisk;
        this.toolRisk = standingOrders.ToolRisk;
        this.toolScope = standingOrders.ToolScope;
        this.explicitlyPermits = standingOrders.ExplicitlyPermits;
        this.identityResolver = standingOrders.IdentityResolver;

        this.perimeterService = perimeterService;
        this.executionService = executionService;
        this.loggingBroker = loggingBroker;

        this.irreversibleToolNames = new HashSet<string>(
            standingOrders.IrreversibleTools, StringComparer.OrdinalIgnoreCase);

        this.enforceSelection = standingOrders.EnforceSelection;

        this.advertisedToolNames = new HashSet<string>(
            standingOrders.AdvertisedTools, StringComparer.OrdinalIgnoreCase);
    }

    public ValueTask<AgentContext> ActAsync(AgentContext context) =>
    TryCatch(async () =>
    {
        ValidateContext(context);

        if (IsTerminal(context.DirectionType))
        {
            string result = await this.executionService.ReturnAsync(context.Payload);

            await this.loggingBroker.LogPayloadAsync(
                "Direction", $"{context.DirectionType} returned", result, detail: false);

            return context with
            {
                Result = result,
                Status = ToTerminalStatus(context.DirectionType)
            };
        }

        // A call naming a caller tool is not an act — it is a terminal answer addressed to the
        // caller (docs/per-request-inference.md §6.2). Classified before the perimeter, because
        // there is nothing for the perimeter to judge: the agent performs nothing.
        if (IsAddressedToCaller(context))
        {
            return await AwaitCallerAsync(context);
        }

        return await ActOnEffectAsync(context);
    });

    // The boundary already dropped any caller tool sharing a configured name, so a name found
    // here has exactly one meaning: the caller's.
    private static bool IsAddressedToCaller(AgentContext context) =>
        context.Inference?.CallerTools.Any(tool =>
            tool.Name.Equals(context.DirectionType, StringComparison.OrdinalIgnoreCase)) is true;

    // The framework already models "the run pauses; something outside this process must act and
    // report back" — built for human approval, structurally identical here. The authority over
    // this pending effect is the caller: it executes, posts the result on the session, and the
    // run resumes. Same seam, different authority.
    private async ValueTask<AgentContext> AwaitCallerAsync(AgentContext context)
    {
        AgentEffect effect = AgentEffect.For(
            runId: AgentRun.Current?.Id ?? string.Empty,
            toolName: context.DirectionType,
            arguments: context.Payload,
            principal: this.identityResolver?.Invoke()) with
        {
            // The id the model minted, preserved so the caller's result can answer it.
            CallId = context.ToolCallId
        };

        await this.loggingBroker.LogProcessAsync(
            "Direction",
            $"Caller tool '{context.DirectionType}' → returned to the caller as a pending effect");

        return context with
        {
            Result = $"'{context.DirectionType}' is addressed to the caller; "
                + "awaiting its result.",

            // The call travels with the pause, so whoever resumes can be shown the act itself
            // rather than only the news that something is waiting (SPEC.md §4.11).
            PendingEffect = effect,
            Status = AgentStatus.AwaitingInput
        };
    }

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
