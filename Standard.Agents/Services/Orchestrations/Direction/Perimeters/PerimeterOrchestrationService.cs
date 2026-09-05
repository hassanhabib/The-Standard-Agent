// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Times;
using Standard.Agents.Models.Brokers.Effects;
using Standard.Agents.Models.Orchestrations.Effects;
using Standard.Agents.Services.Foundations.Approvals;
using Standard.Agents.Services.Foundations.EffectLedgers;
using Standard.Agents.Services.Foundations.Policys;

namespace Standard.Agents.Services.Orchestrations.Direction.Perimeters;

public partial class PerimeterOrchestrationService : IPerimeterOrchestrationService
{
    private readonly IPolicyService policyService;
    private readonly IApprovalService approvalService;
    private readonly IEffectLedgerService effectLedgerService;
    private readonly ITimeBroker timeBroker;
    private readonly ILoggingBroker loggingBroker;

    public PerimeterOrchestrationService(
        IPolicyService policyService,
        IApprovalService approvalService,
        IEffectLedgerService effectLedgerService,
        ITimeBroker timeBroker,
        ILoggingBroker loggingBroker)
    {
        this.policyService = policyService;
        this.approvalService = approvalService;
        this.effectLedgerService = effectLedgerService;
        this.timeBroker = timeBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<AuthorizationDecision> AuthorizeAsync(AgentEffect effect) =>
    TryCatch(async () =>
    {
        AuthorizationDecision decision = await this.policyService.AuthorizeEffectAsync(effect);

        if (decision.Permitted is false)
        {
            await this.loggingBroker.LogProcessAsync(
                "Direction",
                $"Policy → DENIED '{effect.ToolName}': {decision.Reason}");
        }

        return decision;
    });

    // The ledger's record, read as a verdict (SPEC.md §4.9; principal review 2026-09-04, F-08).
    // A completed act is replayed. A live claim by another run is told, not performed. Anything
    // else - a claim past its lease, this run's own earlier attempt with no outcome, a failed
    // tool, a compensation in flight or done - is an act whose fate is unknown, and the only
    // honest answer is to hold the run until a person reconciles the ledger against the world.
    public ValueTask<EffectClaim> ClaimAsync(AgentEffect effect) =>
    TryCatch(async () =>
    {
        EffectRecord? prior = await this.effectLedgerService.ClaimEffectAsync(effect);

        if (prior is null)
        {
            return new EffectClaim(EffectClaimVerdict.Proceed, Outcome: null, Record: null);
        }

        if (prior.State is EffectState.Completed)
        {
            await this.loggingBroker.LogProcessAsync(
                "Direction",
                $"Run-once → '{effect.ToolName}' already ran; replaying its outcome");

            return new EffectClaim(EffectClaimVerdict.Replay, prior.Outcome ?? string.Empty, prior);
        }

        bool liveElsewhere = prior.State is EffectState.InFlight
            && prior.Owner != effect.RunId
            && prior.LeaseUntil > this.timeBroker.GetCurrentDateTimeOffset();

        if (liveElsewhere)
        {
            await this.loggingBroker.LogProcessAsync(
                "Direction",
                $"Run-once → '{effect.ToolName}' is in progress in another run; not performed");

            return new EffectClaim(EffectClaimVerdict.InProgress, Outcome: null, prior);
        }

        await this.loggingBroker.LogProcessAsync(
            "Direction",
            $"Run-once → '{effect.ToolName}' has an earlier attempt with no usable outcome "
                + $"({prior.State}); held for reconciliation");

        return new EffectClaim(EffectClaimVerdict.Unreconciled, Outcome: null, prior);
    });

    public ValueTask<ApprovalDecision> RequestApprovalAsync(AgentEffect effect) =>
    TryCatch(async () =>
    {
        ApprovalDecision approval = await this.approvalService.RequestApprovalAsync(effect);

        if (approval is ApprovalDecision.Approved)
        {
            await this.loggingBroker.LogProcessAsync(
                "Direction", $"Approval → APPROVED '{effect.ToolName}'");
        }

        return approval;
    });

    public ValueTask RecordOutcomeAsync(AgentEffect effect, string outcome) =>
        this.effectLedgerService.RecordOutcomeAsync(effect, outcome);

    public ValueTask RecordFailureAsync(AgentEffect effect, string detail) =>
        this.effectLedgerService.RecordFailureAsync(effect, detail);

    public ValueTask ReleaseClaimAsync(AgentEffect effect) =>
        this.effectLedgerService.ReleaseClaimAsync(effect);

    public ValueTask RecordCompensationIntentAsync(string idempotencyKey) =>
        this.effectLedgerService.RecordCompensationIntentAsync(idempotencyKey);

    public ValueTask RecordCompensationAsync(string idempotencyKey, string detail) =>
        this.effectLedgerService.RecordCompensationAsync(idempotencyKey, detail);
}
