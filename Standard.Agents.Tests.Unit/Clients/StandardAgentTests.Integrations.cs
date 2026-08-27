// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Brokers.Mcps;
using Standard.Agents.Models.Foundations.Skills;
using Xunit;

namespace Standard.Agents.Tests.Unit.Clients;

// The integration rule: what the agent connects TO accumulates, never replaces. Tools always
// did; these tests hold MCP servers and skill sources to the same rule — a second registration
// must not silently unplug the first.
public class StandardAgentIntegrationTests
{
    [Fact]
    public async Task ShouldRouteAcrossMultipleMcpServersByToolNameAsync()
    {
        // given — two servers, each owning one tool; the FIRST registered owns 'alpha'.
        var firstServer = new Mock<IMcpBroker>();

        firstServer.Setup(broker => broker.ListToolsAsync())
            .ReturnsAsync([new McpTool("alpha", "first server's tool")]);

        firstServer.Setup(broker => broker.CallAsync("alpha", It.IsAny<string>()))
            .ReturnsAsync("42 from the first server");

        var secondServer = new Mock<IMcpBroker>();

        secondServer.Setup(broker => broker.ListToolsAsync())
            .ReturnsAsync([new McpTool("beta", "second server's tool")]);

        int turn = 0;

        StandardAgent agent = new StandardAgent()
            .UseMemory(EmptyMemory())
            .UseKnowledge(EmptyKnowledge())
            .UseMcp(firstServer.Object)
            .UseMcp(secondServer.Object)
            .OnBrain(async (systemPrompt, userPrompt) =>
                ++turn is 1 ? "ACTION: alpha: ping" : "FINAL: done");

        // when
        await agent.ProcessPromptAsync("use alpha");

        // then — the call reached the server that owns the name, not merely the last one added.
        firstServer.Verify(broker =>
            broker.CallAsync("alpha", It.IsAny<string>()),
                Times.Once);

        secondServer.Verify(broker =>
            broker.CallAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
    }

    [Fact]
    public async Task ShouldReadSkillsFromEveryRegisteredSourceAsync()
    {
        // given — a broker-backed source and a delegate source, registered in that order.
        var registrySource = new Mock<ISkillBroker>();

        registrySource.Setup(broker => broker.SelectSkillsAsync())
            .ReturnsAsync([new Skill { Name = "alpha", Content = "ALPHA-RULE" }]);

        string? capturedSystemPrompt = null;

        StandardAgent agent = new StandardAgent()
            .UseMemory(EmptyMemory())
            .UseKnowledge(EmptyKnowledge())
            .UseSkills(registrySource.Object)
            .OnSkills(async () => [new Skill { Name = "beta", Content = "BETA-RULE" }])
            .OnBrain(async (systemPrompt, userPrompt) =>
            {
                capturedSystemPrompt = systemPrompt;

                return "FINAL: done";
            });

        // when
        await agent.ProcessPromptAsync("hello");

        // then — both sources reached the brain; the second registration added, not replaced.
        capturedSystemPrompt.Should().Contain("ALPHA-RULE");
        capturedSystemPrompt.Should().Contain("BETA-RULE");
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
