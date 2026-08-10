// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Skills;

namespace Standard.Agents.Brokers.Skills;

public sealed class FileSkillBroker : ISkillBroker
{
    private const string FrontmatterFence = "---";

    private readonly string skillsPath;
    private readonly string searchPattern;

    public FileSkillBroker(string skillsPath, string searchPattern = "*.md")
    {
        this.skillsPath = skillsPath;
        this.searchPattern = searchPattern;
    }

    public async ValueTask<IReadOnlyList<Skill>> SelectSkillsAsync()
    {
        if (Directory.Exists(this.skillsPath) is false)
        {
            return [];
        }

        List<Skill> skills = [];

        foreach (string filePath in Directory.EnumerateFiles(
            this.skillsPath, this.searchPattern, SearchOption.AllDirectories))
        {
            string rawContent = await File.ReadAllTextAsync(filePath);

            string relativeName =
                Path.GetRelativePath(this.skillsPath, filePath).Replace('\\', '/');

            skills.Add(BuildSkill(relativeName, rawContent));
        }

        return skills;
    }

    private static Skill BuildSkill(string relativeName, string rawContent)
    {
        (string? name, string? description, string body) = ParseFrontmatter(rawContent);

        return new Skill
        {
            Name = string.IsNullOrWhiteSpace(name) ? relativeName : name!,
            Description = description ?? string.Empty,
            Content = body
        };
    }

    private static (string? Name, string? Description, string Body) ParseFrontmatter(string content)
    {
        string[] lines = content.Replace("\r\n", "\n").Split('\n');

        if (lines.Length == 0 || lines[0].Trim() != FrontmatterFence)
        {
            return (null, null, content);
        }

        int closingIndex = -1;

        for (int index = 1; index < lines.Length; index++)
        {
            if (lines[index].Trim() == FrontmatterFence)
            {
                closingIndex = index;

                break;
            }
        }

        if (closingIndex == -1)
        {
            return (null, null, content);
        }

        string? name = ReadField(lines, 1, closingIndex, "name");
        string? description = ReadField(lines, 1, closingIndex, "description");
        string body = string.Join("\n", lines[(closingIndex + 1)..]).TrimStart('\n');

        return (name, description, body);
    }

    private static string? ReadField(string[] lines, int start, int end, string key)
    {
        string prefix = key + ":";

        for (int index = start; index < end; index++)
        {
            string trimmedLine = lines[index].Trim();

            if (trimmedLine.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return trimmedLine[prefix.Length..].Trim();
            }
        }

        return null;
    }
}
