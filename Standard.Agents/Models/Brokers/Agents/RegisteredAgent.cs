// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Brokers.Agents;

/// <summary>
/// One agent a registry offers: the name a handoff must use, the description that advertises it
/// (the same opt-in a tool's description is — no description, no advertisement), and the agent
/// itself, already composed by whoever registered it.
/// </summary>
public sealed record RegisteredAgent(string Name, string Description, IAgent Agent);
