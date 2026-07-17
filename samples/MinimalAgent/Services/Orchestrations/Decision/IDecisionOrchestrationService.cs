// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using MinimalAgent.Models;

namespace MinimalAgent.Services.Orchestrations.Decision;

public interface IDecisionOrchestrationService
{
    ValueTask<AgentContext> ThinkAsync(AgentContext context);
}
