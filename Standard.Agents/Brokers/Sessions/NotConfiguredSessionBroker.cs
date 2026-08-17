// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Sessions;

namespace Standard.Agents.Brokers.Sessions;

// The default. SPEC.md §4.11: absent a session, behavior is exactly as if the section did not
// exist — nothing is loaded, nothing is written, and the agent is stateless prompt to prompt.
public sealed class NotConfiguredSessionBroker : ISessionBroker
{
    public ValueTask<AgentSession?> SelectSessionAsync(string sessionId) =>
        ValueTask.FromResult<AgentSession?>(null);

    public ValueTask UpsertSessionAsync(AgentSession session) =>
        ValueTask.CompletedTask;
}
