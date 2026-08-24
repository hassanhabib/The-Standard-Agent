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

    // The streamed door, as server-sent events: the kind as the event name so a consumer can
    // switch on it, the content as data. Filtering the stream to Response events equals what
    // the batched door returns - the parity the loop guarantees, carried out to the protocol.
    [HttpPost("streams")]
    public async ValueTask PostStreamAsync(AgentRunRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;

            return;
        }

        Response.ContentType = "text/event-stream";

        await foreach (AgentStreamEvent streamEvent in
            this.agent.StreamPromptAsync(request.Prompt, HttpContext.RequestAborted))
        {
            string message = $"event: {streamEvent.Type}\ndata: {streamEvent.Content}\n\n";

            await Response.WriteAsync(message, HttpContext.RequestAborted);
            await Response.Body.FlushAsync(HttpContext.RequestAborted);
        }
    }
}
