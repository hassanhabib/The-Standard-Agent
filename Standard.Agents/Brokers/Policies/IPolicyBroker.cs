// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Brokers.Policies;

public interface IPolicyBroker
{
    ValueTask<AuthorizationDecision> AuthorizeAsync(AgentEffect effect);
}
