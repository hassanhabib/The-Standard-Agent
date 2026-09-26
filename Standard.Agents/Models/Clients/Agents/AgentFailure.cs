// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Clients.Agents;

/// <summary>
/// Why a run stopped without delivering an answer (SPEC.md §3.6, v1.14), for the caller that
/// decides what to do next by switching on a code rather than by reading a sentence.
/// </summary>
/// <param name="Category">Which kind of stop it was, in the categories every failure uses.</param>
/// <param name="Code">One of <see cref="AgentFailureCodes"/>, or a code of the host's own.</param>
/// <param name="Message">The sentence a person is shown, the same one the run's result carries.</param>
public sealed record AgentFailure(AgentFailureCategory Category, string Code, string Message);
