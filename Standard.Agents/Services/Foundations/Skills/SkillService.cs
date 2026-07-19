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

    private ValueTask<string> SelectSkillsFromFilesAsync(IFileBroker fileBroker)
    {
        if (fileBroker.DirectoryExists(this.skillsPath) is false)
        {
            return ValueTask.FromResult(string.Empty);
        }

        return ValueTask.FromResult(string.Empty);
    }
}
