// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using MinimalAgent.Brokers.Data;

namespace MinimalAgent.Services.Foundations.Data;

public sealed class SkillService : ISkillService
{
    private readonly ISkillBroker skillBroker;

    public SkillService(ISkillBroker skillBroker) =>
        this.skillBroker = skillBroker;

    public async ValueTask<string> RetrieveSkillsAsync() =>
        await this.skillBroker.SelectSkillsAsync();
}
