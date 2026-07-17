// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using MinimalAgent.Models;

namespace MinimalAgent.Services.Orchestrations.Data;

public interface IDataOrchestrationService
{
    ValueTask<AgentContext> RecallAsync(AgentContext context);
}
