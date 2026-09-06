// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Host.Models.V1;

/// <summary>
/// One past exchange in the caller-owned transcript: what was asked, what was answered, and the
/// calls that turn made to be able to answer.
/// </summary>
/// <remarks>
/// A caller whose protocol is stateless re-posts the whole conversation on every request. Without
/// a per-turn home for the calls, a finished call has nowhere to go but
/// <c>AgentRunRequestV1.ToolExchanges</c>, which is this turn's in-flight work; a call replayed
/// there is read as evidence for the prompt being asked now.
/// </remarks>
public sealed record AgentTurnV1(string Prompt, string Answer)
{
    public IReadOnlyList<ToolExchangeV1> Exchanges { get; init; } = [];
}
