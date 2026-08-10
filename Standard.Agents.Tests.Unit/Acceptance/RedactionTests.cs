// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Skills;
using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

public class RedactionTests
{
    [Fact]
    public async Task ShouldHidePiiFromBrainAndRestoreItInTheAnswerAsync()
    {
        // given
        var generator = new Mock<IGeneratorBroker>();
        generator.Setup(broker =>
            broker.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("FINAL: I've emailed {{EMAIL_0}} the report");

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
            .UseMcp(new Mock<IMcpBroker>().Object)
            .Redact();

        // when
        string answer =
            await agent.ProcessPromptAsync("please email jane@acme.com the report");

        // then — the brain saw a token, never the address
        generator.Verify(broker =>
            broker.GenerateAsync(
                It.IsAny<string>(),
                It.Is<string>(prompt =>
                    prompt.Contains("{{EMAIL_0}}")
                        && prompt.Contains("jane@acme.com") == false)),
                Times.AtLeastOnce);

        // and the caller got the real address back
        answer.Should().Contain("jane@acme.com");
        answer.Should().NotContain("{{EMAIL_0}}");
    }
}
