// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Data;

public sealed class SkillBroker : ISkillBroker
{
    private readonly string skillsPath;

    public SkillBroker(string skillsPath) =>
        this.skillsPath = Path.Combine(AppContext.BaseDirectory, skillsPath);

    public ValueTask<string> SelectSkillsAsync() =>
        ValueTask.FromResult(
            string.Join(
                "\n\n",
                Directory
                    .EnumerateFiles(this.skillsPath, "*.md")
                    .OrderBy(path => path)
                    .Select(File.ReadAllText)));
}
