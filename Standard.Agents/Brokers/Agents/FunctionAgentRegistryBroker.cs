// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Agents;

namespace Standard.Agents.Brokers.Agents;

/// <summary>
/// The Custom mode of the fleet seam: your own code decides which agents exist, per selection —
/// a database of tenants' agents, a feature flag, a directory service.
/// </summary>
public sealed class FunctionAgentRegistryBroker : IAgentRegistryBroker
{
    private readonly Func<ValueTask<IReadOnlyList<RegisteredAgent>>> select;

    public FunctionAgentRegistryBroker(
        Func<ValueTask<IReadOnlyList<RegisteredAgent>>> select) =>
        this.select = select;

    public ValueTask<IReadOnlyList<RegisteredAgent>> SelectAgentsAsync() =>
        this.select();
}
