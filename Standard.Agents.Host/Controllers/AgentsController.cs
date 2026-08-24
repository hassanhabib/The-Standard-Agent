// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Standard.Agents.Host.Models;

namespace Standard.Agents.Host.Controllers;

// The agent, exposed. Pure duplex mapping over ONE service dependency (IAgent) - no business
// logic lives here, because an exposer is disposable and the loop is not.
[ApiController]
[Route("api/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly IAgent agent;

    public AgentsController(IAgent agent) =>
        this.agent = agent;

    [HttpPost("runs")]
    public async ValueTask<ActionResult<AgentRunResponse>> PostRunAsync(AgentRunRequest request)
    {
        throw new NotImplementedException();
    }
}
