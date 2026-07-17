// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Data;
using Standard.Agents.Brokers.Loggings;

namespace Standard.Agents.Services.Foundations.Data;

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
        throw new NotImplementedException();
}
