// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Services.Foundations.EffectLedgers;

public interface IEffectLedgerService
{
    /// <summary>
    /// Claims this act and reports whether it already happened — <c>null</c> when this is the
    /// first time and the caller should proceed (SPEC.md §4.9).
    /// </summary>
    ValueTask<string?> RetrieveOutcomeAsync(AgentEffect effect);

    /// <summary>Records what the act produced, before the loop advances.</summary>
    ValueTask RecordOutcomeAsync(AgentEffect effect, string outcome);

    /// <summary>
    /// Gives back the claim on an act that was held rather than performed. A held act is one that
    /// still has to be able to happen.
    /// </summary>
    ValueTask ReleaseClaimAsync(AgentEffect effect);
}
