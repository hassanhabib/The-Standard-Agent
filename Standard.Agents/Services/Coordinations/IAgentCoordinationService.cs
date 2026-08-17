// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Clients.Agents;

namespace Standard.Agents.Services.Coordinations;

public interface IAgentCoordinationService
{
    ValueTask<string> ProcessPromptAsync(string prompt);

    ValueTask<string> ProcessPromptAsync(string prompt, CancellationToken cancellationToken);

    IAsyncEnumerable<AgentStreamEvent> ProcessPromptStreamAsync(
        string prompt,
        CancellationToken cancellationToken = default);
}
