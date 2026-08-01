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
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Brokers.Verifiers;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Loggings;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

public class TraceTranscriptTests
{
    private static async IAsyncEnumerable<string> ToStream(params string[] tokens)
    {
        foreach (string token in tokens)
        {
            await Task.CompletedTask;

            yield return token;
        }
    }

    private static StandardAgent CreateTracingAgent(string logPath, TraceVerbosity verbosity)
    {
        var generator = new Mock<IGeneratorBroker>();

        generator.Setup(broker => broker.GenerateStreamAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(ToStream("FINAL: 42"));

        generator.Setup(broker => broker.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("FINAL: 42");

        var skills = new Mock<ISkillBroker>();
        skills.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync("Answer directly.");

        var memory = new Mock<IMemoryBroker>();
        memory.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledge = new Mock<IKnowledgeBroker>();
        knowledge.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>())).ReturnsAsync([]);

        var gate = new Mock<IClassifierBroker>();
        gate.Setup(broker => broker.ClassifyAsync(It.IsAny<string>())).ReturnsAsync("allow");

        var judge = new Mock<IVerifierBroker>();
        judge.Setup(broker => broker.VerifyAsync(It.IsAny<string>())).ReturnsAsync("1.0");

        return new StandardAgent()
            .UseGenerator(generator.Object)
            .UseSkills(skills.Object)
            .UseMemory(memory.Object)
            .UseKnowledge(knowledge.Object)
            .UseGate(gate.Object)
            .UseJudge(judge.Object)
            .UseMcp(new Mock<IMcpBroker>().Object)
            .LogTo(logPath, verbosity);
    }

    private static async Task<string> RunAndReadTranscriptAsync(TraceVerbosity verbosity)
    {
        string logPath = Path.GetTempFileName();
        StandardAgent agent = CreateTracingAgent(logPath, verbosity);

        await foreach (AgentStreamEvent _ in agent.StreamPromptAsync("what is the answer?"))
        {
        }

        string transcript = await File.ReadAllTextAsync(logPath);
        File.Delete(logPath);

        return transcript;
    }

    [Fact]
    public async Task ShouldWriteTurnStepProcessTranscriptAtFullVerbosityAsync()
    {
        // given . when
        string transcript = await RunAndReadTranscriptAsync(TraceVerbosity.Full);

        // then
        transcript.Should().Contain("Turn 0");
        transcript.Should().Contain("Step 0: Data");
        transcript.Should().Contain("Process 0: Data: Received prompt");
        transcript.Should().Contain("Step 1: Decision");
        transcript.Should().Contain("Gate");
        transcript.Should().Contain("Judge");
        transcript.Should().Contain("Step 2: Direction");
    }

    [Fact]
    public async Task ShouldOmitStepsAndProcessesAtSummaryVerbosityAsync()
    {
        // given . when
        string transcript = await RunAndReadTranscriptAsync(TraceVerbosity.Summary);

        // then
        transcript.Should().Contain("Turn 0");
        transcript.Should().NotContain("Step 0: Data");
        transcript.Should().NotContain("Process 0:");
    }

    [Fact]
    public async Task ShouldShowNaturesButNotDetailProcessesAtNaturesVerbosityAsync()
    {
        // given . when
        string transcript = await RunAndReadTranscriptAsync(TraceVerbosity.Natures);

        // then
        transcript.Should().Contain("Step 1: Decision");
        transcript.Should().Contain("Gate");
        transcript.Should().NotContain("Retrieved skills");
    }
}
