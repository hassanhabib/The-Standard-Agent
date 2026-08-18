// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents.Services.Orchestrations.Decision.Inferences;

// The model call, and how its answer is read.
//
// One foundation, but a region rather than a pass-through: choosing between the text protocol and
// native tool calling, shaping what the model is shown, and reading its reply back into an intent
// is the substance of this nature. Whether that reading was right is the Guardian's question, not
// this one's.
public interface IInferenceOrchestrationService
{
    /// <summary>
    /// Asks the model and reads its answer into <c>intent</c> / <c>directionType</c> /
    /// <c>payload</c>. Uses native tool calling when a V1 brain is configured, the text protocol
    /// otherwise — the caller sees no difference (SPEC.md §6).
    /// </summary>
    ValueTask<AgentContext> DecideAsync(AgentContext context);

    /// <summary>
    /// The same, streamed. Draft segments are yielded as they arrive; the interpreted result is
    /// handed back through <paramref name="setDecided"/> once the reply is whole.
    /// </summary>
    IAsyncEnumerable<AgentStreamEvent> DecideStreamAsync(
        AgentContext context,
        Action<AgentContext> setDecided,
        CancellationToken cancellationToken);
}
