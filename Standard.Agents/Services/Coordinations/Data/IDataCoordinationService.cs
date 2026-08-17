// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents.Services.Coordinations.Data;

// What the agent HAS — the nature, composed from its two regions.
public interface IDataCoordinationService
{
    ValueTask<AgentContext> RecallAsync(AgentContext context);

    ValueTask RememberAsync(string memory);

    ValueTask<AgentSession?> RecallSessionAsync(string sessionId);

    ValueTask RecordSessionAsync(AgentSession session);
}
