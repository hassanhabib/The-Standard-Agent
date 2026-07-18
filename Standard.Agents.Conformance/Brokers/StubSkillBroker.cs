// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Skills;

namespace Standard.Agents.Conformance;

public sealed class StubSkillBroker : ISkillBroker
{
    public ValueTask<string> SelectSkillsAsync() =>
        ValueTask.FromResult("You are a test agent.");
}
