// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Sessions;

namespace Standard.Agents.Brokers.Sessions;

// The Custom mode's substrate: host-supplied read and write, for a store the host already has.
public sealed class FunctionSessionBroker : ISessionBroker
{
    private readonly Func<string, ValueTask<AgentSession?>> select;
    private readonly Func<AgentSession, ValueTask> upsert;

    public FunctionSessionBroker(
        Func<string, ValueTask<AgentSession?>> select,
        Func<AgentSession, ValueTask> upsert)
    {
        this.select = select;
        this.upsert = upsert;
    }

    public ValueTask<AgentSession?> SelectSessionAsync(string sessionId) =>
        this.select(sessionId);

    public ValueTask UpsertSessionAsync(AgentSession session) =>
        this.upsert(session);
}
