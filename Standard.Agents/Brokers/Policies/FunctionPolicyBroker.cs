// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Brokers.Policies;

// The Custom mode's substrate: a host-supplied function standing in for a broker.
public sealed class FunctionPolicyBroker : IPolicyBroker
{
    private readonly Func<AgentEffect, ValueTask<AuthorizationDecision>> authorize;

    public FunctionPolicyBroker(Func<AgentEffect, ValueTask<AuthorizationDecision>> authorize) =>
        this.authorize = authorize;

    public ValueTask<AuthorizationDecision> AuthorizeAsync(AgentEffect effect) =>
        this.authorize(effect);
}
