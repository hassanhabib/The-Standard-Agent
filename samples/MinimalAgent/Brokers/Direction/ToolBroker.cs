// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using MinimalAgent.Tools;

namespace MinimalAgent.Brokers.Direction;

public sealed class ToolBroker : IToolBroker
{
    private readonly IReadOnlyDictionary<string, ITool> tools;

    public ToolBroker(IEnumerable<ITool> tools) =>
        this.tools = tools.ToDictionary(tool => tool.Name, StringComparer.OrdinalIgnoreCase);

    public bool Has(string name) =>
        this.tools.ContainsKey(name);

    public ValueTask<string> RunAsync(string name, string input) =>
        this.tools[name].ExecuteAsync(input);
}
