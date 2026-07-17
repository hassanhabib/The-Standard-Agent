// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Skills;

public sealed class SkillBroker : ISkillBroker
{
    private const string SkillFilePattern = "*.md";
    private const string SkillSeparator = "\n\n";

    private readonly string skillsPath;

    public SkillBroker(string skillsPath) =>
        this.skillsPath = Path.Combine(AppContext.BaseDirectory, skillsPath);

    public async ValueTask<string> SelectSkillsAsync()
    {
        List<string> skills = [];

        IOrderedEnumerable<string> skillFilePaths =
    Directory.EnumerateFiles(this.skillsPath, SkillFilePattern)
        .OrderBy(skillFilePath => skillFilePath, StringComparer.Ordinal);

        foreach (string skillFilePath in skillFilePaths)
        {
            skills.Add(await File.ReadAllTextAsync(skillFilePath));
        }

        return string.Join(SkillSeparator, skills);
    }
}
