// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Coordinations.Agents;

/// <summary>
/// The host's selection judgment (SPEC.md §4.15): given the run's task and the described tool
/// names, the subset this run is offered. A model carrying a delegate rather than a naked
/// delegate parameter, because configuration crosses natures where a dependency may not —
/// exactly the shape the external tool catalog set.
/// </summary>
public sealed class ToolSelector
{
    private readonly Func<string, IReadOnlyList<string>, ValueTask<IReadOnlyList<string>>> select;

    public ToolSelector(
        Func<string, IReadOnlyList<string>, ValueTask<IReadOnlyList<string>>> select) =>
        this.select = select;

    public ValueTask<IReadOnlyList<string>> SelectAsync(
        string task,
        IReadOnlyList<string> describedToolNames) =>
        this.select(task, describedToolNames);
}
