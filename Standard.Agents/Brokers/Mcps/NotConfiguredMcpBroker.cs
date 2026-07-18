// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Mcps;

public sealed class NotConfiguredMcpBroker : IMcpBroker
{
    public async ValueTask<string> CallAsync(string name, string input) =>
        $"[external '{name}' not configured]";
}
