// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using FluentAssertions;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Clients;

// Found in the 2026-09-04 principal review (F-09): IAgent's primary member was a string in and
// a string out, and its outcome-reporting members defaulted to "the run answered". An agent
// that implemented only the string door had every held or refused run reported to a host or an
// outer agent as an answer. The request-rich members are the contract now; the string members
// are adapters over them, so an adapter can never fabricate a status.
public class AgentContractTests
{
    private const string HeldResult = "waiting on an authority";

    // An agent written against the contract alone — the two request-rich primaries — whose
    // runs are held, exactly the case an adapter must not turn into an answer.
    private sealed class HoldingAgent : IAgent
    {
        public async ValueTask<AgentOutcome> RunAsync(
            PromptRequest request,
            CancellationToken cancellationToken) =>
            new AgentOutcome(Result: HeldResult, Status: AgentStatus.AwaitingApproval);

        public async IAsyncEnumerable<AgentStreamEvent> StreamPromptAsync(
            PromptRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new AgentStreamEvent(AgentStreamEventType.Status, HeldResult);
        }
    }

    [Fact]
    public async Task ShouldReportAHeldRunTruthfullyThroughTheStringRunAdaptersAsync()
    {
        // given
        IAgent agent = new HoldingAgent();

        var expectedOutcome =
            new AgentOutcome(Result: HeldResult, Status: AgentStatus.AwaitingApproval);

        // when
        AgentOutcome actualOutcome = await agent.RunAsync("wire the money");

        AgentOutcome actualStoppableOutcome =
            await agent.RunAsync("wire the money", CancellationToken.None);

        string actualAnswer = await agent.ProcessPromptAsync("wire the money");

        // then: the adapters carried what the run reported, not what they assumed
        actualOutcome.Should().BeEquivalentTo(expectedOutcome);
        actualStoppableOutcome.Should().BeEquivalentTo(expectedOutcome);
        actualAnswer.Should().Be(HeldResult);
    }

    [Fact]
    public async Task ShouldStreamAHeldRunThroughTheStringStreamAdapterAsync()
    {
        // given
        IAgent agent = new HoldingAgent();
        var streamedEvents = new List<AgentStreamEvent>();

        // when
        await foreach (AgentStreamEvent streamEvent in agent.StreamPromptAsync("wire the money"))
        {
            streamedEvents.Add(streamEvent);
        }

        // then
        streamedEvents.Should().ContainSingle(streamEvent =>
            streamEvent.Type == AgentStreamEventType.Status
                && streamEvent.Content == HeldResult);
    }
}
