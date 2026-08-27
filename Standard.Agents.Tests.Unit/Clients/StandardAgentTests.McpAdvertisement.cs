// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Models.Brokers.Mcps;
using Standard.Agents.Models.Foundations.Skills;
using Xunit;

namespace Standard.Agents.Tests.Unit.Clients;

// Discovery (tools/list) already routes calls; these tests make it advertise too. A remote tool
// with a description belongs in the {{tools}} catalog beside the local ones — the same opt-in
// rule (a description is the advertisement), the same catalog line, and best-effort: a server
// down at advertisement time hides only its own tools and never fails the turn.
public class StandardAgentMcpAdvertisementTests
{
    [Fact]
    public async Task ShouldAdvertiseDescribedMcpToolsInTheCatalogAsync()
    {
        // given — a server offering one described and one undescribed tool.
        var server = new Mock<IMcpBroker>();

        server.Setup(broker => broker.ListToolsAsync())
            .ReturnsAsync(
            [
                new McpTool("weather", "answers weather questions"),
                new McpTool("undocumented", "")
            ]);

        string? capturedSystemPrompt = null;

        StandardAgent agent = new StandardAgent()
            .UseMemory(EmptyMemory())
            .UseKnowledge(EmptyKnowledge())
            .OnSkills(async () =>
                [new Skill { Name = "persona", Content = "Your tools:\n{{tools}}" }])
            .UseMcp(server.Object)
            .OnBrain(async (systemPrompt, userPrompt) =>
            {
                capturedSystemPrompt = systemPrompt;

                return "FINAL: done";
            });

        // when
        await agent.ProcessPromptAsync("hello");

        // then — described remote tools advertise; the description opt-in holds for them too.
        capturedSystemPrompt.Should().Contain("weather — answers weather questions");
        capturedSystemPrompt.Should().NotContain("undocumented");
    }

    [Fact]
    public async Task ShouldStillAnswerWhenAdvertisementDiscoveryFailsAsync()
    {
        // given — a server that cannot even list its tools.
        var server = new Mock<IMcpBroker>();

        server.Setup(broker => broker.ListToolsAsync())
            .ThrowsAsync(new HttpRequestException("server is down"));

        StandardAgent agent = new StandardAgent()
            .UseMemory(EmptyMemory())
            .UseKnowledge(EmptyKnowledge())
            .OnSkills(async () =>
                [new Skill { Name = "persona", Content = "Your tools:\n{{tools}}" }])
            .UseMcp(server.Object)
            .OnBrain(async (systemPrompt, userPrompt) => "FINAL: 42");

        // when
        string answer = await agent.ProcessPromptAsync("hello");

        // then — advertisement is best-effort; a down server never fails the turn.
        answer.Should().Be("42");
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
