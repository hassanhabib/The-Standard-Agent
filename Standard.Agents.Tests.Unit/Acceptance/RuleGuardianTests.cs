// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Clients.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

public class RuleGuardianTests
{
    private static async IAsyncEnumerable<string> ToStream(params string[] tokens)
    {
        foreach (string token in tokens)
        {
            await Task.CompletedTask;

            yield return token;
        }
    }

    private static StandardAgent BuildAgent(Mock<IGeneratorBroker> generator)
    {
        var skills = new Mock<ISkillBroker>();
        skills.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(string.Empty);
        var memory = new Mock<IMemoryBroker>();
        memory.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);
        var knowledge = new Mock<IKnowledgeBroker>();
        knowledge.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>())).ReturnsAsync([]);

        return new StandardAgent()
            .UseGenerator(generator.Object)
            .UseSkills(skills.Object)
            .UseMemory(memory.Object)
            .UseKnowledge(knowledge.Object)
            .UseMcp(new Mock<IMcpBroker>().Object)
            .RuleGate("password");
    }

    private static async Task<string> DrainAsync(StandardAgent agent, string prompt)
    {
        string last = string.Empty;

        await foreach (AgentStreamEvent streamEvent in agent.StreamPromptAsync(prompt))
        {
            last = streamEvent.Content;
        }

        return last;
    }

    [Fact]
    public async Task ShouldRefuseDeterministicallyWhenRuleGateMatchesAsync()
    {
        // given
        var generator = new Mock<IGeneratorBroker>();
        generator.Setup(broker => broker.GenerateStreamAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(ToStream("FINAL: your password is hunter2"));

        StandardAgent agent = BuildAgent(generator);

        // when — a prompt that trips the rule
        await DrainAsync(agent, "please reset my PASSWORD");

        // then — the brain is never reached; the gate stopped it deterministically
        generator.Verify(broker => broker.GenerateStreamAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Fact]
    public async Task ShouldAllowThroughWhenRuleGateDoesNotMatchAsync()
    {
        // given
        var generator = new Mock<IGeneratorBroker>();
        generator.Setup(broker => broker.GenerateStreamAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(ToStream("FINAL: Paris"));

        StandardAgent agent = BuildAgent(generator);

        // when — a benign prompt
        await DrainAsync(agent, "capital of France?");

        // then — the brain is reached; the gate let it through
        generator.Verify(broker => broker.GenerateStreamAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
    }
}
