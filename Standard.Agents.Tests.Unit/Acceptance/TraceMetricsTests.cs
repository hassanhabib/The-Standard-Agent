// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Skills;
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
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Loggings;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

public class TraceMetricsTests
{
    private static async IAsyncEnumerable<string> ToStream(params string[] tokens)
    {
        foreach (string token in tokens)
        {
            await Task.CompletedTask;

            yield return token;
        }
    }

    [Fact]
    public async Task ShouldStampElapsedOnRunOutcomeAsync()
    {
        // given
        string logPath = Path.GetTempFileName();

        var generator = new Mock<IGeneratorBroker>();

        generator.Setup(broker => broker.GenerateStreamAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(ToStream("FINAL: 42"));

        var skills = new Mock<ISkillBroker>();
        skills.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill> { new() { Content = "Answer directly." } });
        var memory = new Mock<IMemoryBroker>();
        memory.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);
        var knowledge = new Mock<IKnowledgeBroker>();
        knowledge.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>())).ReturnsAsync([]);
        var gate = new Mock<IClassifierBroker>();
        gate.Setup(broker => broker.ClassifyAsync(It.IsAny<string>())).ReturnsAsync("accept");
        var judge = new Mock<IVerifierBroker>();
        judge.Setup(broker => broker.VerifyAsync(It.IsAny<string>())).ReturnsAsync("1.0");

        var agent = new StandardAgent()
            .UseGenerator(generator.Object)
            .UseSkills(skills.Object)
            .UseMemory(memory.Object)
            .UseKnowledge(knowledge.Object)
            .UseGate(gate.Object)
            .UseJudge(judge.Object)
            .UseMcp(new Mock<IMcpBroker>().Object)
            .LogTo(logPath, TraceVerbosity.Full);

        // when
        await foreach (AgentStreamEvent _ in agent.StreamPromptAsync("what is the answer?"))
        {
        }

        string transcript = await File.ReadAllTextAsync(logPath);
        File.Delete(logPath);

        // then — the run outcome carries an elapsed duration
        transcript.Should().Contain("ms)");
    }
}
