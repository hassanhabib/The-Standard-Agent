// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Effects;

namespace Standard.Agents.Models.Orchestrations.Effects;

/// <summary>
/// The perimeter's answer to "may this act run": the verdict, the recorded outcome when the
/// verdict is to replay it, and the prior record when there is one to reconcile.
/// </summary>
public sealed record EffectClaim(
    EffectClaimVerdict Verdict,
    string? Outcome,
    EffectRecord? Record);
