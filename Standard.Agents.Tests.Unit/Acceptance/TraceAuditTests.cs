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
using Xunit;
using Standard.Agents.Models.Brokers.Generators;

namespace Standard.Agents.Tests.Unit.Acceptance;

public class TraceAuditTests
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
    public async Task ShouldWriteStructuredJsonAuditRecordsAsync()
    {
        // given
        string auditPath = Path.GetTempFileName();

        var generator = new Mock<IGeneratorBroker>();
        generator.Setup(broker => broker.GenerateStreamAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(ToStream("FINAL: 42"));

        generator.Setup(broker => broker.GenerateStreamAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ResolvedInference>(),
            It.IsAny<CancellationToken>()))
                .Returns((
                    string systemPrompt,
                    string userPrompt,
                    ResolvedInference _,
                    CancellationToken token) =>
                        generator.Object.GenerateStreamAsync(systemPrompt, userPrompt, token));

        var skills = new Mock<ISkillBroker>();
        skills.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill> { new() { Content = "Answer directly." } });
        var memory = new Mock<IMemoryBroker>();
        memory.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);
        var knowledge = new Mock<IKnowledgeBroker>();
        knowledge.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>())).ReturnsAsync([]);
        var gate = new Mock<IClassifierBroker>();
        gate.Setup(broker => broker.ClassifyAsync(It.IsAny<string>())).ReturnsAsync("accept");
        var judge = new Mock<IVerifierBroker>();
        judge.Setup(broker => broker.VerifyAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("1.0");

        var agent = new StandardAgent()
            .UseGenerator(generator.Object)
            .UseSkills(skills.Object)
            .UseMemory(memory.Object)
            .UseKnowledge(knowledge.Object)
            .UseGate(gate.Object)
            .UseJudge(judge.Object)
            .UseMcp(new Mock<IMcpBroker>().Object)
            .Audit(auditPath);

        // when
        await foreach (AgentStreamEvent _ in agent.StreamPromptAsync("what is the answer?"))
        {
        }

        string audit = await File.ReadAllTextAsync(auditPath);
        File.Delete(auditPath);

        // then — one JSON object per trace event, machine-ingestible
        audit.Should().Contain("\"kind\":\"turn\"");
        audit.Should().Contain("\"kind\":\"process\"");
        audit.Should().Contain("\"kind\":\"outcome\"");
    }
}
