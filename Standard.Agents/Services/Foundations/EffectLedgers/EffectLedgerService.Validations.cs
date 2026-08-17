// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.EffectLedgers.Exceptions;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Services.Foundations.EffectLedgers;

public partial class EffectLedgerService
{
    // An act with no name cannot be judged, and judging it anyway would return a decision about
    // nothing that the perimeter would then act on (SPEC.md §4.9).
    private static void ValidateEffect(AgentEffect effect)
    {
        if (effect is null || string.IsNullOrWhiteSpace(effect.ToolName))
        {
            throw new InvalidEffectLedgerException(
                message: "Invalid effect ledger. Please correct the error and try again.");
        }
    }
}
