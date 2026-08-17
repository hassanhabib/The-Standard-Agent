// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Brokers.Loggings;
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
    private readonly ILoggingBroker loggingBroker;

    public PerimeterOrchestrationService(
        IPolicyService policyService,
        IApprovalService approvalService,
        IEffectLedgerService effectLedgerService,
        ILoggingBroker loggingBroker)
    {
        this.policyService = policyService;
        this.approvalService = approvalService;
        this.effectLedgerService = effectLedgerService;
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

    public ValueTask<string?> ClaimAsync(AgentEffect effect) =>
    TryCatch(async () =>
    {
        string? priorOutcome = await this.effectLedgerService.RetrieveOutcomeAsync(effect);

        if (priorOutcome is not null)
        {
            await this.loggingBroker.LogProcessAsync(
                "Direction",
                $"Run-once → '{effect.ToolName}' already ran; replaying its outcome");
        }

        return priorOutcome;
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

    public ValueTask ReleaseClaimAsync(AgentEffect effect) =>
        this.effectLedgerService.ReleaseClaimAsync(effect);
}
