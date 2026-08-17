// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Orchestrations.Agents;

public enum AgentStatus
{
    Working = 0,
    Responded = 1,
    AwaitingInput = 2,
    Refused = 3,
    Failed = 4,
    Revising = 5,

    // Terminal for the turn: an effect is proposed but not performed, because an authority has
    // not answered yet (SPEC.md §3.1, §4.9). Distinct from AwaitingInput — the agent is not
    // asking the user a question, it is asking for permission, and waiting is not consent.
    AwaitingApproval = 6
}
