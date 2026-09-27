// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Usages;

namespace Standard.Agents.Models.Clients.Agents;

public sealed record AgentStreamEvent(
    AgentStreamEventType Type,
    string Content)
{
    /// <summary>
    /// The run's usage so far, on a <see cref="AgentStreamEventType.Usage"/> event, and nothing on
    /// every other kind. Carried whole rather than only as the number in <see cref="Content"/>,
    /// because a count is either reported or estimated (SPEC.md §3.4) and the text cannot say
    /// which.
    /// </summary>
    public AgentUsage? Usage { get; init; }
}
