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
using Standard.Agents.Models.Brokers.Generators;

namespace Standard.Agents.Tests.Unit.Acceptance;

public class SkillNestedDirectoryTests
{
    [Fact]
    public async Task ShouldLoadSkillFromNestedDirectoryAndStripFrontmatterAsync()
    {
        // given — a skill nested one folder deep, with YAML frontmatter
        string root = Path.Combine(Path.GetTempPath(), "skills-" + Guid.NewGuid().ToString("N"));
        string nested = Path.Combine(root, "the-standard-skill");
        Directory.CreateDirectory(nested);

        await File.WriteAllTextAsync(
            Path.Combine(nested, "SKILL.md"),
            "---\nname: the-standard-skill\ndescription: does the standard thing\n---\n"
                + "Always answer in a haiku.");

        string capturedSystemPrompt = string.Empty;

        var generator = new Mock<IGeneratorBroker>();
        generator.Setup(broker =>
            broker.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((systemPrompt, _) => capturedSystemPrompt = systemPrompt)
                .ReturnsAsync("FINAL: ok");

        generator.Setup(broker => broker.GenerateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ResolvedInference>()))
                .Returns((string systemPrompt, string userPrompt, ResolvedInference _) =>
                    generator.Object.GenerateAsync(systemPrompt, userPrompt));

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

        // then — the nested skill's body reached the brain, frontmatter stripped
        capturedSystemPrompt.Should().Contain("Always answer in a haiku.");
        capturedSystemPrompt.Should().NotContain("description: does the standard thing");
    }
}
