// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Mcps;

namespace Standard.Agents.Conformance;

public sealed class NotConfiguredMcpBroker : IMcpBroker
{
    public async ValueTask<string> CallAsync(string name, string input) =>
        $"[external '{name}' not configured]";
}
