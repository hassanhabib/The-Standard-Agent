// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Mcps;

namespace Standard.Agents.Brokers.Mcps;

public sealed class StdioMcpBroker : IMcpBroker
{
    public StdioMcpBroker(TextReader serverOutput, TextWriter serverInput, int timeoutSeconds) =>
        throw new NotImplementedException();

    public ValueTask<string> CallAsync(string name, string argumentsJson) =>
        throw new NotImplementedException();

    public ValueTask<IReadOnlyList<McpTool>> ListToolsAsync() =>
        throw new NotImplementedException();
}
