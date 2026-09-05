// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Effects;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Services.Foundations.EffectLedgers;

public interface IEffectLedgerService
{
    /// <summary>
    /// Claims this act for its run, in flight, under a lease, and reports the prior record when
    /// the key already has one — <c>null</c> when this is the first time and the caller should
    /// proceed (SPEC.md §4.9).
    /// </summary>
    ValueTask<EffectRecord?> ClaimEffectAsync(AgentEffect effect);

    /// <summary>Records what the act produced, before the loop advances: the record is completed.</summary>
    ValueTask RecordOutcomeAsync(AgentEffect effect, string outcome);

    /// <summary>
    /// Records that the tool threw. Whether the world changed is unknown, so the record is failed
    /// rather than released, and a repeat is held for reconciliation.
    /// </summary>
    ValueTask RecordFailureAsync(AgentEffect effect, string detail);

    /// <summary>
    /// Gives back the claim on an act that was held rather than performed — only an in-flight
    /// claim; a record with an outcome names an act that happened.
    /// </summary>
    ValueTask ReleaseClaimAsync(AgentEffect effect);

    /// <summary>Records that compensation was decided, before it is attempted.</summary>
    ValueTask RecordCompensationIntentAsync(string idempotencyKey);

    /// <summary>Records that the act was undone, and how.</summary>
    ValueTask RecordCompensationAsync(string idempotencyKey, string detail);
}
