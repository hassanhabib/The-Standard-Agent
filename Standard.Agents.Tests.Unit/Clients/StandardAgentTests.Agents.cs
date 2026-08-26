// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Models.Brokers.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Clients;

// The fleet: registered agents materialize as tools at composition. That one decision buys
// everything the framework already knows how to do — the advertisement opt-in, the perimeter,
// the audit, cancellation across the seam — without a second code path for "agent" beside
// "tool". A handoff is an act, and the same door governs every act.
public class StandardAgentAgentsTests
{
    [Fact]
    public async Task ShouldHandOffToARegisteredAgentAsync()
    {
        // given — a registered specialist that captures what it was actually handed.
        string? innerReceived = null;

        StandardAgent billingAgent = new StandardAgent()
            .UseMemory(EmptyMemory())
            .UseKnowledge(EmptyKnowledge())
            .OnBrain(async (systemPrompt, userPrompt) =>
            {
                innerReceived = userPrompt;

                return "FINAL: the refund is booked";
            });

        int turn = 0;

        StandardAgent outerAgent = new StandardAgent()
            .UseMemory(EmptyMemory())
            .UseKnowledge(EmptyKnowledge())
            .OnAgents(() => new ValueTask<IReadOnlyList<RegisteredAgent>>(
                [new RegisteredAgent("billing", "Handles refunds and invoices.", billingAgent)]))
            .OnBrain(async (systemPrompt, userPrompt) =>
                ++turn is 1
                    ? "ACTION: billing: refund order 7741"
                    : "FINAL: done — billing booked the refund.");

        // when
        string answer = await outerAgent.ProcessPromptAsync(
            "please refund my last order, number 7741");

        // then — the handoff was grounded: the task the outer brain wrote AND the user's
        // original ask, which is the default sharing ruling (task + just enough context).
        innerReceived.Should().Contain("refund order 7741");
        innerReceived.Should().Contain("please refund my last order, number 7741");
        answer.Should().Contain("billing booked the refund");
    }

    [Fact]
    public async Task ShouldAdvertiseRegisteredAgentsToTheBrainAsync()
    {
        // given — a registry whose agent carries a description, which is the advertisement
        // opt-in a tool's description already is.
        StandardAgent specialist = new StandardAgent()
            .UseMemory(EmptyMemory())
            .UseKnowledge(EmptyKnowledge())
            .OnBrain(async (systemPrompt, userPrompt) => "FINAL: ok");

        string? outerSaw = null;

        StandardAgent outerAgent = new StandardAgent()
            .UseMemory(EmptyMemory())
            .UseKnowledge(EmptyKnowledge())
            .OnAgents(() => new ValueTask<IReadOnlyList<RegisteredAgent>>(
                [new RegisteredAgent("billing", "Handles refunds and invoices.", specialist)]))
            .OnBrain(async (systemPrompt, userPrompt) =>
            {
                outerSaw = systemPrompt;

                return "FINAL: nothing to do";
            });

        // when
        await outerAgent.ProcessPromptAsync("hello");

        // then — the brain can only hand off to an agent it was told exists.
        outerSaw.Should().Contain("billing");
        outerSaw.Should().Contain("Handles refunds and invoices.");
    }

    private static IMemoryBroker EmptyMemory()
    {
        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        return memoryBroker.Object;
    }

    private static IKnowledgeBroker EmptyKnowledge()
    {
        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        return knowledgeBroker.Object;
    }
}
