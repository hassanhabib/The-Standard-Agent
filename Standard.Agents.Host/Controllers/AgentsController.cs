// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text;
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
    // CR, LF and CRLF all end a line in server-sent events; CRLF first so it splits as one.
    private static readonly string[] lineTerminators = ["\r\n", "\n", "\r"];

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
            string frame = ToServerSentEvent(streamEvent);

            await Response.WriteAsync(frame, HttpContext.RequestAborted);
            await Response.Body.FlushAsync(HttpContext.RequestAborted);
        }
    }

    // One frame per event, in the shape the SSE specification reads: the kind as the event name,
    // one "data:" line per line of content, then the blank line that ends the event. A content
    // line left unprefixed is read as a field, and a blank line inside the content ends the event
    // early - which is what a multi-line answer, the normal case, used to do (principal review
    // 2026-09-04, F-11). The consumer joins the data lines back with a newline.
    private static string ToServerSentEvent(AgentStreamEvent streamEvent)
    {
        string[] contentLines = streamEvent.Content.Split(lineTerminators, StringSplitOptions.None);
        var frame = new StringBuilder();

        frame.Append("event: ").Append(streamEvent.Type).Append('\n');

        foreach (string contentLine in contentLines)
        {
            frame.Append("data: ").Append(contentLine).Append('\n');
        }

        frame.Append('\n');

        return frame.ToString();
    }
}
