// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Audits;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Foundations.Skills;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// The decision log end to end, through the Local mode — a file (SPEC.md §4.7).
//
// The regression these pin is the one the enterprise plan opens on: the audit file used to be
// truncated at the start of every run, so a sink only ever held the last prompt and a regulator
// asking what the agent did yesterday got nothing.
public class DecisionLogTests : IDisposable
{
    private readonly string auditPath =
        Path.Combine(Path.GetTempPath(), $"decision-log-{Guid.NewGuid():n}.jsonl");

    private StandardAgent AnsweringAgent()
    {
        var skillBroker = new Mock<ISkillBroker>();

        skillBroker.Setup(broker => broker.SelectSkillsAsync())
            .ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .OnBrain(async (systemPrompt, userPrompt) => "FINAL: 42")
            .Audit(this.auditPath);
    }

    private static string RunIdOf(string line) =>
        JsonDocument.Parse(line).RootElement.GetProperty("runId").GetString()!;

    private static int SequenceOf(string line) =>
        JsonDocument.Parse(line).RootElement.GetProperty("sequence").GetInt32();

    [Fact]
    public async Task ShouldRetainEveryRunInTheDecisionLogOnProcessPromptAsync()
    {
        // given
        StandardAgent agent = AnsweringAgent();

        // when
        await agent.ProcessPromptAsync(prompt: "the ALPHA question");
        await agent.ProcessPromptAsync(prompt: "the BRAVO question");

        // then
        string[] actualLines = await File.ReadAllLinesAsync(this.auditPath);

        actualLines.Should().Contain(line => line.Contains("ALPHA"));
        actualLines.Should().Contain(line => line.Contains("BRAVO"));

        actualLines.Select(RunIdOf).Distinct().Should().HaveCount(2);
    }

    [Fact]
    public async Task ShouldNumberEachRunFromZeroInTheDecisionLogOnProcessPromptAsync()
    {
        // given
        StandardAgent agent = AnsweringAgent();

        // when
        await agent.ProcessPromptAsync(prompt: "first");
        await agent.ProcessPromptAsync(prompt: "second");

        // then
        string[] actualLines = await File.ReadAllLinesAsync(this.auditPath);

        IEnumerable<IGrouping<string, string>> runs = actualLines.GroupBy(RunIdOf);

        foreach (IGrouping<string, string> run in runs)
        {
            int[] sequences = run.Select(SequenceOf).ToArray();

            sequences.Should().BeInAscendingOrder();
            sequences.Should().StartWith(0);
            sequences.Should().OnlyHaveUniqueItems();
        }
    }

    [Fact]
    public async Task ShouldDetectAnAlteredOrRemovedRecordInTheDecisionLogAsync()
    {
        // given
        StandardAgent agent = AnsweringAgent();

        await agent.ProcessPromptAsync(prompt: "the question");

        string[] writtenLines = await File.ReadAllLinesAsync(this.auditPath);

        string[] alteredLines = [.. writtenLines];
        alteredLines[0] = alteredLines[0].Replace("run started", "nothing happened");

        string[] shortenedLines = [.. writtenLines.Skip(1)];

        // when
        int? actualIntactLink = FileAuditBroker.FindBrokenLink(writtenLines);
        int? actualAlteredLink = FileAuditBroker.FindBrokenLink(alteredLines);
        int? actualShortenedLink = FileAuditBroker.FindBrokenLink(shortenedLines);

        // then
        actualIntactLink.Should().BeNull();
        actualAlteredLink.Should().NotBeNull();
        actualShortenedLink.Should().NotBeNull();
    }

    // Sequential runs only, deliberately. SPEC.md §4.7 also requires that CONCURRENT runs stay
    // attributable, and this release does not satisfy that: LoggingBroker holds runId and
    // sequence as instance fields, and one broker is composed per agent, so two prompts in
    // flight on one instance overwrite each other's run identity. Fixing it moves run identity
    // out of the broker's state, which is Run Isolation's work — and it is a conformance
    // prerequisite for the decision log, not an enhancement to it. The concurrency assertion
    // lands there, with the rest of the isolation guarantee.

    public void Dispose()
    {
        if (File.Exists(this.auditPath))
        {
            File.Delete(this.auditPath);
        }
    }
}
