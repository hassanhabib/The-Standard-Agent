// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.IO;
using FluentAssertions;
using Moq;
using Standard.Agents;
using Standard.Agents.Brokers.Classifiers;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Brokers.Verifiers;
using Standard.Agents.Models.Loggings;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

public class TraceFailureTests
{
    [Fact]
    public async Task ShouldWriteFailureToTranscriptWhenBrainThrowsAsync()
    {
        // given
        string logPath = Path.GetTempFileName();

        var generator = new Mock<IGeneratorBroker>();

        generator.Setup(broker => broker.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("brain exploded"));

        var skills = new Mock<ISkillBroker>();
        skills.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync("Answer directly.");
        var memory = new Mock<IMemoryBroker>();
        memory.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);
        var knowledge = new Mock<IKnowledgeBroker>();
        knowledge.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>())).ReturnsAsync([]);
        var gate = new Mock<IClassifierBroker>();
        gate.Setup(broker => broker.ClassifyAsync(It.IsAny<string>())).ReturnsAsync("accept");

        var agent = new StandardAgent()
            .UseGenerator(generator.Object)
            .UseSkills(skills.Object)
            .UseMemory(memory.Object)
            .UseKnowledge(knowledge.Object)
            .UseGate(gate.Object)
            .UseMcp(new Mock<IMcpBroker>().Object)
            .LogTo(logPath, TraceVerbosity.Full);

        // when
        try
        {
            await agent.ProcessPromptAsync("what is the answer?");
        }
        catch
        {
            // the failure is expected; we are asserting it was traced
        }

        string transcript = await File.ReadAllTextAsync(logPath);
        File.Delete(logPath);

        // then — a thrown failure must appear in the audit trace, not vanish
        transcript.Should().Contain("ERROR");
    }
}
