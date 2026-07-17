// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using MinimalAgent.Models;

namespace MinimalAgent.Services.Orchestrations.Direction;

public interface IDirectionOrchestrationService
{
    ValueTask<AgentContext> ActAsync(AgentContext context);
}
