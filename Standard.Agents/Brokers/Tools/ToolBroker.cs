// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Tools;

namespace Standard.Agents.Brokers.Tools;

public sealed class ToolBroker : IToolBroker
{
    private readonly IReadOnlyDictionary<string, ITool> tools;

    public ToolBroker(IEnumerable<ITool> tools) =>
    this.tools = tools.ToDictionary(
        tool => tool.Name,
        StringComparer.OrdinalIgnoreCase);

    public async ValueTask<bool> HasAsync(string name) =>
        this.tools.ContainsKey(name);

    public async ValueTask<string> RunAsync(string name, string input) =>
        await this.tools[name].ExecuteAsync(input);
}
