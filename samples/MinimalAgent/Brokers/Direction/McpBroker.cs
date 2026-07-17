// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace MinimalAgent.Brokers.Direction;

public sealed class McpBroker : IMcpBroker
{
    public ValueTask<string> CallAsync(string name, string input) =>
        ValueTask.FromResult($"[external '{name}' not configured]");
}
