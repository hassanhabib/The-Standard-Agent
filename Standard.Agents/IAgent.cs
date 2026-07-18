// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Clients.Agents;

namespace Standard.Agents;

public interface IAgent
{
    ValueTask<string> ProcessPromptAsync(string prompt);

    IAsyncEnumerable<AgentStreamEvent> StreamPromptAsync(
        string prompt,
        CancellationToken cancellationToken = default);
}
