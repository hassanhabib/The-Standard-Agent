// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Brokers.Effects;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Policies;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Effects;
using Standard.Agents.Services.Foundations.ExternalTools;
using Standard.Agents.Services.Foundations.Gates;
using Standard.Agents.Services.Foundations.InternalTools;
using Standard.Agents.Services.Foundations.Returns;

namespace Standard.Agents.Services.Orchestrations.Direction;

public partial class DirectionOrchestrationService : IDirectionOrchestrationService
{
    private const string ReturnResponseDirection = "ReturnResponse";
    private const string RefuseDirection = "Refuse";
    private const string AwaitInputDirection = "AwaitInput";

    private readonly IInternalToolService internalToolService;
    private readonly IExternalToolService externalToolService;
    private readonly IReturnService returnService;
    private readonly ILoggingBroker loggingBroker;
    private readonly IPolicyBroker policyBroker;
    private readonly IApprovalBroker approvalBroker;
    private readonly IEffectLedgerBroker effectLedgerBroker;
    private readonly IGateService? screeningService;
    private readonly HashSet<string> irreversibleToolNames;

    // Asked once per act rather than captured once at composition, because who is acting can
    // change between prompts on the same agent — a singleton serving many callers is the shape
    // this framework asks hosts to adopt (SPEC.md §4.4).
    private readonly Func<AgentPrincipal?>? identityResolver;

    public DirectionOrchestrationService(
        IInternalToolService internalToolService,
        IExternalToolService externalToolService,
        IReturnService returnService,
        ILoggingBroker loggingBroker,
        IEnumerable<string>? allowedTools = null,
        IPolicyBroker? policyBroker = null,
        IApprovalBroker? approvalBroker = null,
        IEffectLedgerBroker? effectLedgerBroker = null,
        IEnumerable<string>? irreversibleTools = null,
        IGateService? screeningService = null,
        Func<AgentPrincipal?>? identityResolver = null)
    {
        this.screeningService = screeningService;
        this.identityResolver = identityResolver;

        this.internalToolService = internalToolService;
        this.externalToolService = externalToolService;
        this.returnService = returnService;
        this.loggingBroker = loggingBroker;

        // The allow-list is now expressed as a policy, so the simple answer and an external
        // policy engine travel one seam and a denial carries a reason either way (SPEC.md §4.9).
        this.policyBroker = policyBroker
            ?? (allowedTools is null
                ? new NotConfiguredPolicyBroker()
                : new AllowListPolicyBroker(allowedTools));

        this.approvalBroker = approvalBroker ?? new NotConfiguredApprovalBroker();
        this.effectLedgerBroker = effectLedgerBroker ?? new InMemoryEffectLedgerBroker();

        this.irreversibleToolNames =
            new HashSet<string>(irreversibleTools ?? [], StringComparer.OrdinalIgnoreCase);
    }

    public ValueTask<AgentContext> ActAsync(AgentContext context) =>
    TryCatch(async () =>
    {
        ValidateContext(context);

        if (IsTerminal(context.DirectionType))
        {
            string result = await this.returnService.ReturnAsync(context.Payload);

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
