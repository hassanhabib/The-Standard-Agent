// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents.Services.Orchestrations.Decision;

internal sealed class DecisionStream : IDecisionStream
{
    private readonly IAsyncEnumerable<AgentStreamEvent> events;

    public AgentContext Result { get; private set; } = new();

    public DecisionStream(
        Func<Action<AgentContext>, IAsyncEnumerable<AgentStreamEvent>> build) =>
        this.events = build(context => this.Result = context);

    public IAsyncEnumerator<AgentStreamEvent> GetAsyncEnumerator(
        CancellationToken cancellationToken = default) =>
        this.events.GetAsyncEnumerator(cancellationToken);
}
