// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Orchestrations.Effects;

/// <summary>
/// What happens to an act that nothing explicitly permitted.
/// </summary>
/// <remarks>
/// Explicit permissions — <c>AllowTools</c>, a policy broker, <c>RequireApproval</c> — always
/// answer first. This is only the disposition toward everything they did not mention, which is
/// the case that cannot be enumerated in advance: an agent touching files cannot have its targets
/// listed at composition, so the interesting question is what it does about the ones it meets.
/// </remarks>
public enum PermissionMode
{
    /// <summary>
    /// Permitted. The default, and what every release before this one did — an unlisted tool ran
    /// without being asked about, which is right for a chat agent and wrong for an agent with
    /// hands.
    /// </summary>
    Open = 0,

    /// <summary>Requires approval. Ask-first: the posture an agent with hands should run under.</summary>
    Ask = 1,

    /// <summary>Denied. Nothing runs but what was named.</summary>
    Deny = 2
}
