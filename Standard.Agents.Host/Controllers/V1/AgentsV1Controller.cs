// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Standard.Agents.Host.Models.V1;

namespace Standard.Agents.Host.Controllers.V1;

// The agent, exposed in full: version 1 of the host's wire contract carries everything a run
// is — session, transcript, caller tools, resumed exchanges, inference controls — and reports
// everything a run ends with, the pending act included. Pure duplex mapping over ONE service
// dependency (IAgent); the prompt-only api/agents routes stay as the convenience door.
[ApiController]
[Route("api/V1/agents")]
public class AgentsV1Controller : ControllerBase
{
    private readonly IAgent agent;

    public AgentsV1Controller(IAgent agent) =>
        this.agent = agent;

    [HttpPost("runs")]
    public ValueTask<ActionResult<AgentRunResponseV1>> PostRunAsync(AgentRunRequestV1 request) =>
        throw new NotImplementedException();

    [HttpPost("streams")]
    public ValueTask PostStreamAsync(AgentRunRequestV1 request) =>
        throw new NotImplementedException();
}
