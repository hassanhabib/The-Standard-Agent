// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Services.Foundations.Policys;

public interface IPolicyService
{
    /// <summary>May this act happen, and if not, why not (SPEC.md §4.9)?</summary>
    ValueTask<AuthorizationDecision> AuthorizeEffectAsync(AgentEffect effect);
}
