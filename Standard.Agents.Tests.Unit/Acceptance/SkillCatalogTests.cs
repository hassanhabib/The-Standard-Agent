// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.IO;
using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Brokers.Memorys;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

public class SkillCatalogTests
{
    [Fact]
    public async Task ShouldExpandSkillsMarkerIntoTheDescribedSkillCatalogAsync()
    {
        // given — a base skill with the {{skills}} marker + a described specialist skill
        string root = Path.Combine(Path.GetTempPath(), "skills-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "weather-skill"));

        await File.WriteAllTextAsync(
            Path.Combine(root, "00-base.md"),
            "You are helpful.\n\nAvailable skills:\n{{skills}}");

        await File.WriteAllTextAsync(
            Path.Combine(root, "weather-skill", "SKILL.md"),
            "---\nname: weather\ndescription: answers weather questions\n---\n"
                + "Give a three-day forecast.");

        string capturedSystemPrompt = string.Empty;

        var generator = new Mock<IGeneratorBroker>();
        generator.Setup(broker =>
            broker.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((systemPrompt, _) => capturedSystemPrompt = systemPrompt)
                .ReturnsAsync("FINAL: ok");

        var memory = new Mock<IMemoryBroker>();
        memory.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);
        var knowledge = new Mock<IKnowledgeBroker>();
        knowledge.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>())).ReturnsAsync([]);

        var agent = new StandardAgent()
            .UseGenerator(generator.Object)
            .UseMemory(memory.Object)
            .UseKnowledge(knowledge.Object)
            .UseMcp(new Mock<IMcpBroker>().Object)
            .Skills(root);

        // when
        await agent.ProcessPromptAsync("hello");
        Directory.Delete(root, recursive: true);

        // then — the marker expanded into the described-skill index, and is gone
        capturedSystemPrompt.Should().Contain("- weather — answers weather questions");
        capturedSystemPrompt.Should().NotContain("{{skills}}");
    }
}
