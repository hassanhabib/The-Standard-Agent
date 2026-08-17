// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Skills;

namespace Standard.Agents.Brokers.Skills;

// The Custom mode's substrate for skills (SPEC.md §4.8).
public sealed class FunctionSkillBroker : ISkillBroker
{
    private readonly Func<ValueTask<IReadOnlyList<Skill>>> select;

    public FunctionSkillBroker(Func<ValueTask<IReadOnlyList<Skill>>> select) =>
        this.select = select;

    public ValueTask<IReadOnlyList<Skill>> SelectSkillsAsync() =>
        this.select();
}
