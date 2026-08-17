// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Sessions;

namespace Standard.Agents.Brokers.Sessions;

public interface ISessionBroker
{
    ValueTask<AgentSession?> SelectSessionAsync(string sessionId);

    ValueTask UpsertSessionAsync(AgentSession session);
}
