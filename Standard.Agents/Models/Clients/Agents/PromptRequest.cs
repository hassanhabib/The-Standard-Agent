// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Generators.V1;
using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents.Models.Clients.Agents;

/// <summary>
/// What one caller asked for, on one request (docs/per-request-inference.md §3). Exists at the
/// entry's signature and never travels below it: the loop carries <c>ResolvedInference</c>, the
/// output of precedence, so no tier can ever see a value precedence discarded.
/// </summary>
/// <remarks>
/// A record, not an interface: a request does not <i>do</i> anything, it <i>is</i> something.
/// Deliberately absent — executable tools, permissions, budget, redaction, approvals, principal.
/// A request has no field in which to ask for them, which is a stronger guarantee than a rule
/// that could be forgotten. <see cref="CallerTools"/> is not the exception it appears to be: it
/// grants the model vocabulary, never the agent capability (§6 of the design).
/// </remarks>
public sealed record PromptRequest
{
    public string Prompt { get; init; } = "";

    // Session identity is data about the request and rides the record. The CancellationToken
    // does not — it is runtime control, not a statement about the request — so the entry keeps
    // it as a parameter.
    public string SessionId { get; init; } = "";

    /// <summary>
    /// The caller-owned transcript: what was said before, oldest first. The exposed protocols
    /// this seam exists for are stateless — the client re-posts the conversation — and a run
    /// that cannot receive it starts from nothing every time. When a session exists it wins:
    /// the deployment's record of the conversation beats the caller's retelling of it, the
    /// same precedence every field on this record obeys.
    /// </summary>
    public IReadOnlyList<AgentTurn> History { get; init; } = [];

    /// <summary>
    /// Tool calls the caller already executed and is answering — the second half of the yield:
    /// a run ended AwaitingInput holding a caller's call, the caller ran it, and this is the
    /// result coming back, still naming the call id the model minted.
    /// </summary>
    public IReadOnlyList<ToolExchange> ToolExchanges { get; init; } = [];

    /// <summary>The shape the answer must take. Null means the caller expressed no opinion.</summary>
    public string? ResponseSchemaJson { get; init; }

    // Nullable throughout, because "unset" must be representable — precedence depends on
    // distinguishing a value the caller chose from one they never mentioned.
    public double? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public int? Seed { get; init; }
    public IReadOnlyList<string> Stop { get; init; } = [];

    /// <summary>
    /// Tools the CALLER will execute, declared so the model may name them. The agent never runs
    /// one — there is no code path from this list to Direction's registry. A returned call naming
    /// one is a terminal answer addressed to the caller, not an act.
    /// </summary>
    public IReadOnlyList<ToolDefinition> CallerTools { get; init; } = [];

    // What the core cannot model and should not try to: chat_template_kwargs is vLLM's, thinking
    // is Anthropic's, grammar (GBNF) is llama.cpp's. Carried opaquely, never read by the core,
    // handed to the broker whole — under the core-owned-keys merge rule (§4.4 of the design).
    public string? ProviderOptionsJson { get; init; }
}
