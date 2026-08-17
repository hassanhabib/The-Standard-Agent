// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Services.Orchestrations.Direction.Executions;

// Perform it, or answer.
//
// The three ways an act leaves the agent: a local tool, a tool across the boundary, or the
// terminal that hands the result back and touches nothing.
public interface IExecutionOrchestrationService
{
    /// <summary>True when a local tool answers to this name.</summary>
    ValueTask<bool> HandlesLocallyAsync(string toolName);

    ValueTask<string> RunAsync(string toolName, string payload);

    ValueTask<string> ReturnAsync(string payload);

    /// <summary>
    /// Undoes a local act, or reports that it stands. External acts cannot be undone from here.
    /// </summary>
    ValueTask<string> CompensateAsync(string toolName, string arguments, string outcome);
}
