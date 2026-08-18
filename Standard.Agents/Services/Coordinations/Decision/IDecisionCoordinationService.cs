// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents.Services.Coordinations.Decision;

public interface IDecisionCoordinationService
{
    ValueTask<AgentContext> ThinkAsync(AgentContext context);

    IDecisionStream ThinkStreamAsync(
        AgentContext context,
        CancellationToken cancellationToken = default);
}
