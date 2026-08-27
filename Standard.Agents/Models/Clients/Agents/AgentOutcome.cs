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
public record AgentOutcome(string Result, AgentStatus Status);
