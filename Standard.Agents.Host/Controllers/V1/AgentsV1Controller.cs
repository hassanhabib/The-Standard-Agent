// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text;
using Microsoft.AspNetCore.Mvc;
using Standard.Agents.Host.Models.V1;
using Standard.Agents.Models.Brokers.Generators.V1;
using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Host.Controllers.V1;

// The agent, exposed in full: version 1 of the host's wire contract carries everything a run
// is — session, transcript, caller tools, resumed exchanges, inference controls — and reports
// everything a run ends with, the pending act included. Pure duplex mapping over ONE service
// dependency (IAgent); the prompt-only api/agents routes stay as the convenience door
// (principal review 2026-09-04, F-10).
[ApiController]
[Route("api/V1/agents")]
public class AgentsV1Controller : ControllerBase
{
    // CR, LF and CRLF all end a line in server-sent events; CRLF first so it splits as one.
    private static readonly string[] lineTerminators = ["\r\n", "\n", "\r"];

    private readonly IAgent agent;

    public AgentsV1Controller(IAgent agent) =>
        this.agent = agent;

    [HttpPost("runs")]
    public async ValueTask<ActionResult<AgentRunResponseV1>> PostRunAsync(
        AgentRunRequestV1 request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest("prompt is required");
        }

        // The caller closing the connection cancels the run at its next turn boundary - and,
        // through the nesting seam, any sub-agents the run started.
        AgentOutcome outcome =
            await this.agent.RunAsync(ToPromptRequest(request), HttpContext.RequestAborted);

        return Ok(ToResponse(outcome));
    }

    // The streamed door, as server-sent events: the kind as the event name, the content as
    // data, one "data:" line per line of content. Filtering the stream to Response events
    // equals what the batched door returns.
    [HttpPost("streams")]
    public async ValueTask PostStreamAsync(AgentRunRequestV1 request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;

            return;
        }

        Response.ContentType = "text/event-stream";

        await foreach (AgentStreamEvent streamEvent in
            this.agent.StreamPromptAsync(ToPromptRequest(request), HttpContext.RequestAborted))
        {
            string frame = ToServerSentEvent(streamEvent);

            await Response.WriteAsync(frame, HttpContext.RequestAborted);
            await Response.Body.FlushAsync(HttpContext.RequestAborted);
        }
    }

    // The wire to the contract, field for field. A list the caller leaves out of the document
    // is empty; a field the caller sets to null is refused as 400 naming the field, by the
    // contract the record declares — so nothing here has to guess what null meant.
    private static PromptRequest ToPromptRequest(AgentRunRequestV1 request)
    {
        return new PromptRequest
        {
            Prompt = request.Prompt,
            SessionId = request.SessionId,

            History = [.. request.History.Select(turn =>
                new AgentTurn(turn.Prompt, turn.Answer))],

            ToolExchanges = [.. request.ToolExchanges.Select(exchange =>
                new ToolExchange(
                    exchange.CallId,
                    exchange.ToolName,
                    exchange.ArgumentsJson,
                    exchange.Result))],

            ResponseSchemaJson = request.ResponseSchemaJson,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            Seed = request.Seed,
            Stop = request.Stop,

            CallerTools = [.. request.CallerTools.Select(tool =>
                new ToolDefinition(tool.Name, tool.Description, tool.ParametersJson))],

            ProviderOptionsJson = request.ProviderOptionsJson
        };
    }

    private static AgentRunResponseV1 ToResponse(AgentOutcome outcome)
    {
        return new AgentRunResponseV1(
            Result: outcome.Result,
            Status: outcome.Status.ToString(),
            PendingEffect: ToPendingEffect(outcome.PendingEffect));
    }

    private static PendingEffectV1? ToPendingEffect(AgentEffect? effect)
    {
        if (effect is null)
        {
            return null;
        }

        return new PendingEffectV1(
            RunId: effect.RunId,
            CallId: effect.CallId,
            ToolName: effect.ToolName,
            Arguments: effect.Arguments,
            Scope: effect.Scope,
            RiskLevel: effect.RiskLevel.ToString(),
            ApprovalRequired: effect.ApprovalRequired,
            IdempotencyKey: effect.IdempotencyKey,
            Principal: effect.Principal);
    }

    // One frame per event, in the shape the SSE specification reads (F-11): a content line
    // left unprefixed is read as a field, and a blank line inside the content ends the event.
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
