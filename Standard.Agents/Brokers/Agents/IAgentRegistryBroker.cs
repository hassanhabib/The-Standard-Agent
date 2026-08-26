// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Agents;

namespace Standard.Agents.Brokers.Agents;

/// <summary>
/// The fleet seam: where other agents come from — a folder of agent documents, a provider's
/// registry, a delegate. Registered agents materialize as tools, so a handoff is an act and
/// the perimeter that governs acts governs handoffs.
/// </summary>
public interface IAgentRegistryBroker
{
    ValueTask<IReadOnlyList<RegisteredAgent>> SelectAgentsAsync();
}
