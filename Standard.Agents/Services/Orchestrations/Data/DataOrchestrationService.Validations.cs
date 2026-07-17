// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Agents.Exceptions;

namespace Standard.Agents.Services.Orchestrations.Data;

public partial class DataOrchestrationService
{
    // Circuit-breaking: a null context has nothing to read and nothing to copy, so
    // continuing would only produce a null-reference somewhere less obvious.
    private static void ValidateContext(AgentContext context)
    {
        if (context is null)
        {
            throw new NullAgentContextException(
                message: "Agent context is null.");
        }
    }
}
