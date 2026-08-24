// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Mcps;

namespace Standard.Agents.Brokers.Mcps;

/// <summary>
/// Many MCP servers behind the one seam the agent already speaks. A call routes to the server
/// whose catalog owns the name; when two servers claim the same name, the first registered
/// wins — deterministic, and the same precedence internal tools already have over external
/// ones. Catalogs are discovered lazily and cached per server on success only, so a server
/// that is down at first use is asked again rather than remembered as empty.
/// </summary>
public sealed class CompositeMcpBroker : IMcpBroker
{
    private readonly IReadOnlyList<IMcpBroker> brokers;
    private readonly IReadOnlyList<McpTool>?[] catalogs;
    private readonly SemaphoreSlim discoveryLock = new(initialCount: 1, maxCount: 1);

    public CompositeMcpBroker(IEnumerable<IMcpBroker> brokers)
    {
        this.brokers = [.. brokers];
        this.catalogs = new IReadOnlyList<McpTool>?[this.brokers.Count];
    }

    public async ValueTask<string> CallAsync(string name, string input)
    {
        IMcpBroker? owner = await ResolveAsync(name);

        return owner is null
            ? $"[external '{name}' not configured]"
            : await owner.CallAsync(name, input);
    }

    public async ValueTask<IReadOnlyList<McpTool>> ListToolsAsync()
    {
        await DiscoverAsync();

        // First-registered wins here too, so the union a caller sees is the union the router
        // routes by.
        var seen = new HashSet<string>();
        List<McpTool> union = [];

        foreach (IReadOnlyList<McpTool>? catalog in this.catalogs)
        {
            foreach (McpTool tool in catalog ?? [])
            {
                if (seen.Add(tool.Name))
                {
                    union.Add(tool);
                }
            }
        }

        return union;
    }

    private async ValueTask<IMcpBroker?> ResolveAsync(string name)
    {
        await DiscoverAsync();

        for (int index = 0; index < this.brokers.Count; index++)
        {
            if (this.catalogs[index]?.Any(tool => tool.Name == name) is true)
            {
                return this.brokers[index];
            }
        }

        return null;
    }

    private async ValueTask DiscoverAsync()
    {
        if (this.catalogs.All(catalog => catalog is not null))
        {
            return;
        }

        await this.discoveryLock.WaitAsync();

        try
        {
            for (int index = 0; index < this.brokers.Count; index++)
            {
                if (this.catalogs[index] is not null)
                {
                    continue;
                }

                try
                {
                    this.catalogs[index] = await this.brokers[index].ListToolsAsync();
                }
                catch
                {
                    // A server unreachable at discovery keeps its slot empty and is asked again
                    // on the next call — its outage must not take the other servers' tools with
                    // it, and must not be cached as "has no tools".
                }
            }
        }
        finally
        {
            this.discoveryLock.Release();
        }
    }
}
