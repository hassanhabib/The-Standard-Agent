// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Clients.Agents;

public enum AgentStreamEventType
{
    Status,
    Thinking,
    Tool,
    Response,

    // User-voiced progress prose ("Let me check the web..."), screened before it is emitted.
    // Distinct from Status, which is machine-voiced, and from Thinking, which is unvetted draft.
    Narration
}
