// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Services.Orchestrations.Direction.Perimeters;

// May this act happen, and has it already?
//
// Three foundations, one question asked in three parts: is the principal permitted, does an
// authority consent, and did this exact act already run. The ORDER they are asked in belongs to
// Direction, not here — this region answers, it does not sequence (SPEC.md §4.9).
public interface IPerimeterOrchestrationService
{
    ValueTask<AuthorizationDecision> AuthorizeAsync(AgentEffect effect);

    /// <summary>
    /// Claims the act and reports the outcome of the first execution, or <c>null</c> when this is
    /// the first time and the caller should proceed.
    /// </summary>
    ValueTask<string?> ClaimAsync(AgentEffect effect);

    ValueTask<ApprovalDecision> RequestApprovalAsync(AgentEffect effect);

    ValueTask RecordOutcomeAsync(AgentEffect effect, string outcome);

    /// <summary>Gives back the claim on an act that was held rather than performed.</summary>
    ValueTask ReleaseClaimAsync(AgentEffect effect);
}
