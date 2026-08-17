// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Sessions;

namespace Standard.Agents.Services.Orchestrations.Data.Recollections;

// What the agent accumulated, replayed.
//
// Memory and the session share a region for the same reason skills and knowledge do: both are
// written by the agent's own past rather than by an author, and both are replayed rather than
// searched. Memory is what it learned across conversations; the session is this one.
public interface IRecollectionOrchestrationService
{
    ValueTask<IReadOnlyList<string>> RecallMemoriesAsync();

    ValueTask RememberAsync(string memory);

    ValueTask<AgentSession?> RecallSessionAsync(string sessionId);

    ValueTask RecordSessionAsync(AgentSession session);
}
