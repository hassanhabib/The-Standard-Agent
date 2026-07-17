// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Mcps;

public sealed class NotConfiguredMcpBroker : IMcpBroker
{
    public ValueTask<string> CallAsync(string name, string input) =>
        ValueTask.FromResult($"[external '{name}' not configured]");
}
