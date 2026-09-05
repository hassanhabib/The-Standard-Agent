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

    /// <summary>
    /// The remote tools the agent's servers offer — part of what the agent HAS, so the loop can
    /// judge them with the local tools when it decides what a run is offered (SPEC.md §4.15).
    /// </summary>
    ValueTask<IReadOnlyList<Models.Brokers.Mcps.McpTool>> RetrieveRemoteToolsAsync();
}
