// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Mcps;

namespace Standard.Agents.Conformance;

public sealed class NotConfiguredMcpBroker : IMcpBroker
{
    public async ValueTask<string> CallAsync(string name, string argumentsJson) =>
        $"[external '{name}' not configured]";

    public async ValueTask<IReadOnlyList<Standard.Agents.Models.Brokers.Mcps.McpTool>>
        ListToolsAsync() => [];
}
