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

    ValueTask<string> ProcessPromptAsync(
        string prompt,
        string sessionId,
        CancellationToken cancellationToken);

    IAsyncEnumerable<AgentStreamEvent> ProcessPromptStreamAsync(
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The streamed loop, in a conversation. Every control the batched loop enforces — budgets,
    /// cancellation, sessions — holds here too: a control a caller can step around by changing
    /// method is not a control (SPEC.md §7.6, §4.10, §4.11).
    /// </summary>
    IAsyncEnumerable<AgentStreamEvent> ProcessPromptStreamAsync(
        string prompt,
        string sessionId,
        CancellationToken cancellationToken);
}
