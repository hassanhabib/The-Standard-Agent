// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Services.Orchestrations.Direction;

public interface IDirectionOrchestrationService
{
    ValueTask<AgentContext> ActAsync(AgentContext context);

    /// <summary>
    /// Unwinds what this run performed, in reverse order (SPEC.md §4.9). Reports every effect it
    /// could not undo rather than reporting the run cleanly unwound.
    /// </summary>
    ValueTask<IReadOnlyList<CompensationOutcome>> CompensateRunAsync();
}
