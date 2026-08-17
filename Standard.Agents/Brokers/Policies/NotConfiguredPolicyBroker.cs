// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Brokers.Policies;

// The default. SPEC.md §4.9: absent every perimeter control, behavior is exactly as if the
// section did not exist — so with no policy configured, every registered tool is runnable.
public sealed class NotConfiguredPolicyBroker : IPolicyBroker
{
    public ValueTask<AuthorizationDecision> AuthorizeAsync(AgentEffect effect) =>
        ValueTask.FromResult(AuthorizationDecision.Allow());
}
