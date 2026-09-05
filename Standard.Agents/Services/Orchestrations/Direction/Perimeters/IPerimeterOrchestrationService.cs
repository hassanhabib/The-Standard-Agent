// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Services.Orchestrations.Direction.Perimeters;

public interface IPerimeterOrchestrationService
{
    ValueTask<AuthorizationDecision> AuthorizeAsync(AgentEffect effect);

    /// <summary>
    /// Claims the act and says what the ledger knows about it: proceed, replay a recorded
    /// outcome, another run is on it, or an earlier attempt needs reconciling (SPEC.md §4.9).
    /// </summary>
    ValueTask<EffectClaim> ClaimAsync(AgentEffect effect);

    ValueTask<ApprovalDecision> RequestApprovalAsync(AgentEffect effect);

    ValueTask RecordOutcomeAsync(AgentEffect effect, string outcome);

    /// <summary>Records that the tool threw, so the act's unknown fate is on the record.</summary>
    ValueTask RecordFailureAsync(AgentEffect effect, string detail);

    /// <summary>Gives back the claim on an act that was held rather than performed.</summary>
    ValueTask ReleaseClaimAsync(AgentEffect effect);

    ValueTask RecordCompensationIntentAsync(string idempotencyKey);

    ValueTask RecordCompensationAsync(string idempotencyKey, string detail);
}
