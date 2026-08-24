// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;
using Standard.Agents.Host.Models;
using Standard.Agents.Models.Clients.Agents;

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
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest("prompt is required");
        }

        // The caller closing the connection cancels the run at its next turn boundary - and,
        // through the nesting seam, any sub-agents the run started.
        AgentOutcome outcome =
            await this.agent.RunAsync(request.Prompt, HttpContext.RequestAborted);

        return Ok(new AgentRunResponse(
            Result: outcome.Result,
            Status: outcome.Status.ToString()));
    }
}
