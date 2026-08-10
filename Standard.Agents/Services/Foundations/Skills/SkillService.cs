// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Foundations.Skills;

namespace Standard.Agents.Services.Foundations.Skills;

public partial class SkillService : ISkillService
{
    private const string SkillSeparator = "\n\n";

    private readonly ISkillBroker skillBroker;
    private readonly ILoggingBroker loggingBroker;

    public SkillService(
        ISkillBroker skillBroker,
        ILoggingBroker loggingBroker)
    {
        this.skillBroker = skillBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<string> RetrieveSkillsAsync(string route = "") =>
    TryCatch(async () =>
    {
        IReadOnlyList<Skill> skills = await this.skillBroker.SelectSkillsAsync();

        IEnumerable<Skill> selectedSkills =
            skills.OrderBy(skill => skill.Name, StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(route) is false)
        {
            List<Skill> routedSkills = selectedSkills
                .Where(skill => skill.Name.Contains(route, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (routedSkills.Count > 0)
            {
                selectedSkills = routedSkills;
            }
        }

        return string.Join(SkillSeparator, selectedSkills.Select(skill => skill.Content));
    });
}
