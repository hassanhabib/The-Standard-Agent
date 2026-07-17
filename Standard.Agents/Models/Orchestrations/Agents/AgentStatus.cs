// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Orchestrations.Agents;

public enum AgentStatus
{
    // Working is first so it is the default — the loop runs while a context is Working.
    Working = 0,
    Responded = 1,
    AwaitingInput = 2,
    Refused = 3,
    Failed = 4
}
