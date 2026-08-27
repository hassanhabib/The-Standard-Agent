// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents.Models.Clients.Agents;

/// <summary>
/// How a run ended, and what it produced.
/// </summary>
/// <param name="Result">The answer, or the reason there is not one.</param>
/// <param name="Status">
/// Which way it ended. <see cref="AgentStatus.Responded"/> is the only one that makes the result an
/// answer. Every other ending produces prose about why — a run out of turns says so, with
/// <see cref="AgentStatus.Working"/> because it stopped mid-work, and never hands back a tool's raw
/// output as though it were an answer. A caller that cannot tell these apart will eventually report
/// unfinished work as done.
/// </param>
/// <param name="PendingEffect">
/// The act the run is waiting on, when it ended waiting — a caller's tool call, or an act held
/// for approval. It rides the session too (SPEC.md §4.11), but a stateless deployment has no
/// session, and an exposer that cannot reach the pending call cannot yield it to the caller —
/// which is the whole mechanism (docs/per-request-inference.md §6.2). Null when the run is not
/// waiting on anything.
/// </param>
public record AgentOutcome(
    string Result,
    AgentStatus Status,
    Standard.Agents.Models.Orchestrations.Effects.AgentEffect? PendingEffect = null);
