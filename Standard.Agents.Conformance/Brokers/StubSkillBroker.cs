// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Foundations.Skills;

namespace Standard.Agents.Conformance;

public sealed class StubSkillBroker : ISkillBroker
{
    public async ValueTask<IReadOnlyList<Skill>> SelectSkillsAsync() =>
        [new Skill { Name = "test", Content = "You are a test agent." }];
}
