// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Tools;

namespace Standard.Agents.Brokers.Direction;

public sealed class ToolBroker : IToolBroker
{
    private readonly IReadOnlyDictionary<string, ITool> tools;

    // Tool names come from a model's text output, so matching is case-insensitive.
    public ToolBroker(IEnumerable<ITool> tools) =>
        this.tools = tools.ToDictionary(
            tool => tool.Name,
            StringComparer.OrdinalIgnoreCase);

    public ValueTask<bool> HasAsync(string name) =>
        ValueTask.FromResult(this.tools.ContainsKey(name));
}
