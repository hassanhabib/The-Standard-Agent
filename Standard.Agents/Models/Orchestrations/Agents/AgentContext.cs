// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Models.Orchestrations.Agents;

public sealed record AgentContext
{
    public string Prompt { get; init; } = "";

    // The conversation this prompt belongs to, and what was said before (SPEC.md §3.2, §4.11).
    // Empty when no session is configured, which is exactly the stateless agent that existed
    // before sessions did.
    public string SessionId { get; init; } = "";
    public IReadOnlyList<AgentTurn> History { get; init; } = [];

    public string SystemPrompt { get; init; } = "";
    public IReadOnlyList<string> Observations { get; init; } = [];

    // What a native call asked for and what came back, kept as a pair (SPEC.md §6). Observations
    // alone cannot express this: they are prose, and prose cannot say WHICH call a result answers.
    // On the V0 text path this stays empty and observations remain the whole record, which is the
    // limitation that path has always had.
    public IReadOnlyList<ToolExchange> ToolExchanges { get; init; } = [];

    // The call Direction is performing this turn, so its result can be tied back to it. Empty
    // outside the native path.
    public string ToolCallId { get; init; } = "";

    // The act an authority has been asked to permit and has not yet answered on (SPEC.md §4.9,
    // §4.11). It rides out to the session so a different process can show a human the act itself
    // rather than only the news that something is waiting.
    public AgentEffect? PendingEffect { get; init; }

    public string Intent { get; init; } = "";
    public string Route { get; init; } = "";
    public string DirectionType { get; init; } = "";
    public string Payload { get; init; } = "";
    public string RawReply { get; init; } = "";

    // What the turn's model calls cost. The provider's own report when there is one, counted by
    // the Usage foundation when there is not — because a provider that reports nothing used to
    // leave these at zero, and a budget bounding zero is not a budget (SPEC.md §3.4).
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }

    // Which of those two it was. An estimate is enough to ENFORCE a bound and not enough to
    // RECONCILE one against an invoice, so the distinction travels with the number instead of
    // being inferred from which brain happened to be configured.
    public bool UsageIsEstimated { get; init; }

    public string Result { get; init; } = "";
    public string Remember { get; init; } = "";
    public AgentStatus Status { get; init; }
}
