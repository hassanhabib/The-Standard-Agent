// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Skills;

namespace Standard.Agents.Brokers.Skills;

/// <summary>
/// Many skill sources — folders, registries, delegates — as the one stream the agent reads. The
/// sources are read in registration order and their skills concatenate, so what a skill file
/// relies on (earlier files establish context for later ones) holds across sources exactly as
/// it holds across files within one folder.
/// </summary>
public sealed class CompositeSkillBroker : ISkillBroker
{
    private readonly IReadOnlyList<ISkillBroker> brokers;

    public CompositeSkillBroker(IEnumerable<ISkillBroker> brokers) =>
        this.brokers = [.. brokers];

    public async ValueTask<IReadOnlyList<Skill>> SelectSkillsAsync()
    {
        List<Skill> skills = [];

        foreach (ISkillBroker broker in this.brokers)
        {
            skills.AddRange(await broker.SelectSkillsAsync());
        }

        return skills;
    }
}
