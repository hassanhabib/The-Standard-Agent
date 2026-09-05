// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Mcps;

namespace Standard.Agents.Brokers.Mcps;

public interface IMcpBroker
{
    ValueTask<string> CallAsync(string name, string argumentsJson);

    /// <summary>
    /// The server's tool catalog (<c>tools/list</c>). What makes more than one server composable:
    /// a router can only send a name to the server that owns it if servers say what they own.
    /// </summary>
    ValueTask<IReadOnlyList<McpTool>> ListToolsAsync();
}
