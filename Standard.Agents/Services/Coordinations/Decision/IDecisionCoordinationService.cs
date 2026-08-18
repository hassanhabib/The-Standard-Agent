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

    /// <summary>
    /// Screens a piece of untrusted text and returns the Gate's verdict. Decision owns the
    /// guardians, so anything that needs screening asks Decision for it rather than reaching
    /// into Decision's foundations — which is what Direction used to do.
    /// </summary>
    ValueTask<string> ScreenAsync(string text);
}
