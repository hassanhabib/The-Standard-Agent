// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Models.Brokers.Mcps;

namespace Standard.Agents.Conformance;

// A remote tool server, scripted: its catalog is the tools the vector gave it, every one
// described, and every call is counted — routing across servers is certified by the owner
// being called and the bystander not (SPEC.md §4.8 v1.5).
public sealed class ScriptedMcpServer : IMcpBroker
{
    private readonly Dictionary<string, string> tools;
    private readonly Dictionary<string, string> schemas;
    private int callCount;

    public ScriptedMcpServer(
        Dictionary<string, string> tools,
        Dictionary<string, string>? schemas = null)
    {
        this.tools = tools;
        this.schemas = schemas ?? [];
    }

    public int CallCount => Volatile.Read(ref this.callCount);

    public async ValueTask<string> CallAsync(string name, string argumentsJson)
    {
        Interlocked.Increment(ref this.callCount);

        return this.tools.TryGetValue(name, out string? reply)
            ? reply
            : $"[external '{name}' not configured]";
    }

    // A tool the vector gave a schema advertises it; the rest take an open object, as a real
    // server that declares none would.
    public async ValueTask<IReadOnlyList<McpTool>> ListToolsAsync() =>
        [.. this.tools.Keys.Select(name =>
            new McpTool(
                name,
                $"scripted tool {name}",
                this.schemas.TryGetValue(name, out string? schema) ? schema : "{}"))];
}
