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
/// answer — a run that was held on an authority, refused, or ran out of turns produced prose about
/// why, and a caller that cannot tell those apart will eventually report held work as done.
/// </param>
public record AgentOutcome(string Result, AgentStatus Status);
