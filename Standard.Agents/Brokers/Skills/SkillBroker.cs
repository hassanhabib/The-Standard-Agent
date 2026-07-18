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
        string[] skills = Directory.Exists(this.skillsPath)
            ? await Task.WhenAll(
                Directory.EnumerateFiles(this.skillsPath, SkillFilePattern)
                    .OrderBy(skillFilePath => skillFilePath, StringComparer.Ordinal)
                    .Select(path => File.ReadAllTextAsync(path)))
            : [];

        return string.Join(SkillSeparator, skills);
    }
}
