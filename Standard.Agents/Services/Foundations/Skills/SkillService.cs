// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Files;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Skills;

namespace Standard.Agents.Services.Foundations.Skills;

public partial class SkillService : ISkillService
{
    private const string SkillFilePattern = "*.md";
    private const string SkillSeparator = "\n\n";

    private readonly ISkillBroker? skillBroker;
    private readonly IFileBroker? fileBroker;
    private readonly string skillsPath;
    private readonly ILoggingBroker loggingBroker;

    public SkillService(
        ISkillBroker skillBroker,
        ILoggingBroker loggingBroker)
    {
        this.skillBroker = skillBroker;
        this.skillsPath = string.Empty;
        this.loggingBroker = loggingBroker;
    }

    public SkillService(
        IFileBroker fileBroker,
        string skillsPath,
        ILoggingBroker loggingBroker)
    {
        this.fileBroker = fileBroker;
        this.skillsPath = skillsPath;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<string> RetrieveSkillsAsync() =>
    TryCatch(async () =>
    {
        return this.fileBroker is not null
            ? await SelectSkillsFromFilesAsync(this.fileBroker)
            : await this.skillBroker!.SelectSkillsAsync();
    });

    private async ValueTask<string> SelectSkillsFromFilesAsync(IFileBroker fileBroker)
    {
        if (fileBroker.DirectoryExists(this.skillsPath) is false)
        {
            return string.Empty;
        }

        IOrderedEnumerable<string> skillFilePaths =
            fileBroker.SelectFiles(this.skillsPath, SkillFilePattern, SearchOption.TopDirectoryOnly)
                .OrderBy(skillFilePath => skillFilePath, StringComparer.Ordinal);

        List<string> skills = [];

        foreach (string skillFilePath in skillFilePaths)
        {
            skills.Add(await fileBroker.ReadFileAsync(skillFilePath));
        }

        return string.Join(SkillSeparator, skills);
    }
}
