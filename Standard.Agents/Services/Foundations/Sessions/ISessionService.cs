// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Sessions;

namespace Standard.Agents.Services.Foundations.Sessions;

public interface ISessionService
{
    /// <summary>
    /// The conversation so far, or <c>null</c> when this is the first prompt in it.
    /// </summary>
    ValueTask<AgentSession?> RetrieveSessionAsync(string sessionId);

    /// <summary>Writes the conversation back, replacing what was there.</summary>
    ValueTask RecordSessionAsync(AgentSession session);
}
