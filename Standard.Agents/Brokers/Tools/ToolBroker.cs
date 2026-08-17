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

    // A tool that declares no way back returns null; the broker hands the natures an empty string,
    // so nothing above it has to reason about null (SPEC.md §4.9).
    public async ValueTask<string> CompensateAsync(string name, string input, string outcome) =>
        await this.tools[name].CompensateAsync(input, outcome) ?? string.Empty;
}
