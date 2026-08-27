// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Foundations.Skills;

namespace Standard.Agents.Evals;

// The case's own data, served through the real broker seams so the run under evaluation is
// the real composition, not a shortcut around it.

public sealed class CaseSkillBroker : ISkillBroker
{
    private readonly string skill;

    public CaseSkillBroker(string skill) =>
        this.skill = skill;

    public async ValueTask<IReadOnlyList<Skill>> SelectSkillsAsync() =>
        [new Skill { Name = "case", Content = this.skill }];
}

public sealed class CaseMemoryBroker : IMemoryBroker
{
    private readonly IReadOnlyList<string> memories;

    public CaseMemoryBroker(IReadOnlyList<string> memories) =>
        this.memories = memories;

    public async ValueTask<IReadOnlyList<string>> SelectMemoriesAsync() =>
        this.memories;

    public ValueTask InsertMemoryAsync(string memory) =>
        ValueTask.CompletedTask;
}

public sealed class NotConfiguredMcpBroker : IMcpBroker
{
    public async ValueTask<string> CallAsync(string name, string input) =>
        $"[external '{name}' not configured]";

    public async ValueTask<IReadOnlyList<Standard.Agents.Models.Brokers.Mcps.McpTool>>
        ListToolsAsync() => [];
}
