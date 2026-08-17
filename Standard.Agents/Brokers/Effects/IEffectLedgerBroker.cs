// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Brokers.Effects;

public interface IEffectLedgerBroker
{
    /// <summary>
    /// Records that this effect is about to run, and reports whether it already has. Intent is
    /// written <b>before</b> the act, never after: an effect that executed but was never
    /// recorded is indistinguishable from one that never ran (SPEC.md §4.9).
    /// </summary>
    /// <returns>
    /// The outcome of the first execution when this key has run before; <c>null</c> when this is
    /// the first time and the caller should proceed.
    /// </returns>
    ValueTask<string?> SelectOutcomeAsync(AgentEffect effect);

    /// <summary>Records what the effect produced, before the loop advances.</summary>
    ValueTask InsertOutcomeAsync(AgentEffect effect, string outcome);

    /// <summary>
    /// Gives back a claim on an act that was held rather than performed (SPEC.md §4.9).
    /// </summary>
    /// <remarks>
    /// A held act is one that still has to be able to happen. Leaving its claim standing builds
    /// an approval that can only ever be granted too late: the authority says yes, the resumed
    /// run proposes the act, and the ledger reports it as already done.
    /// </remarks>
    ValueTask DeleteClaimAsync(AgentEffect effect);
}
