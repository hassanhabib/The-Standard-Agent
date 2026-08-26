// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Tools;

// Handoff sharing is a template (SPEC §6.1): {input} is what the outer model wrote, {prompt}
// is what the user originally asked. The grounded default carries both — the task, and just
// enough context to do it — and a custom template decides exactly what crosses, which is the
// whole configurability of a handoff.
public class AgentToolHandoffTests
{
    [Fact]
    public async Task ShouldGroundHandoffWithTheOriginalPromptAsync()
    {
        // given — an inner agent that captures what it was actually handed.
        string? innerReceived = null;

        StandardAgent innerAgent = new StandardAgent()
            .UseMemory(EmptyMemory())
            .UseKnowledge(EmptyKnowledge())
            .OnBrain(async (systemPrompt, userPrompt) =>
            {
                innerReceived = userPrompt;

                return "FINAL: the refund is booked";
            });

        var handoffTool = new AgentTool(
            name: "billing",
            agent: innerAgent,
            handoff: "The user asked: {prompt}\n\nYour task: {input}",
            description: "handles refunds and invoices");

        int turn = 0;

        StandardAgent outerAgent = new StandardAgent()
            .UseMemory(EmptyMemory())
            .UseKnowledge(EmptyKnowledge())
            .Tool(handoffTool)
            .OnBrain(async (systemPrompt, userPrompt) =>
                ++turn is 1
                    ? "ACTION: billing: refund order 7741"
                    : "FINAL: done — billing booked the refund.");

        // when
        await outerAgent.ProcessPromptAsync("please refund my last order, number 7741");

        // then — the sub-agent received the task AND the user's original ask, not the task alone.
        innerReceived.Should().Contain("refund order 7741");
        innerReceived.Should().Contain("please refund my last order, number 7741");
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
