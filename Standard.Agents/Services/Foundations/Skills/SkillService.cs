// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Skills;

namespace Standard.Agents.Services.Foundations.Skills;

public partial class SkillService : ISkillService
{
    private readonly ISkillBroker skillBroker;
    private readonly ILoggingBroker loggingBroker;

    public SkillService(
        ISkillBroker skillBroker,
        ILoggingBroker loggingBroker)
    {
        this.skillBroker = skillBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<string> RetrieveSkillsAsync() =>
    TryCatch(async () =>
        await this.skillBroker.SelectSkillsAsync());
    }
