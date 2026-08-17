// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Effects;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Sessions;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Foundations.Skills;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// Resumption across processes (SPEC.md §4.9, §4.11). Run-once inside one instance is the easy
// half. The half an auditor asks about is the run that was killed immediately after the transfer
// went out, and the process that picked the session up afterwards.
public class ResumptionTests : IDisposable
{
    private readonly string workingPath =
        Path.Combine(Path.GetTempPath(), $"standard-agent-resume-{Guid.NewGuid():n}");

    private string SessionsPath => Path.Combine(this.workingPath, "sessions");

    private string LedgerPath => Path.Combine(this.workingPath, "ledger");

    public void Dispose()
    {
        if (Directory.Exists(this.workingPath))
        {
            Directory.Delete(this.workingPath, recursive: true);
        }
    }

    private sealed class CountingTool : ITool
    {
        public string Name => "wire_transfer";
        public string Description => "Moves money.";
        public string Parameters => "{}";

        public int ExecutionCount { get; private set; }

        public ValueTask<string> ExecuteAsync(string input)
        {
            ExecutionCount++;

            return ValueTask.FromResult("transfer complete");
        }
    }

    // A separate instance each time, wired to the same folders — the closest a unit test comes to
    // a different process, and the thing an in-memory ledger could never demonstrate.
    private StandardAgent NewProcess(CountingTool tool, params string[] replies)
    {
        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        var scriptedReplies = new Queue<string>(replies);

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .Tool(tool)
            .Sessions(SessionsPath)
            .UseEffectLedger(new FileEffectLedgerBroker(LedgerPath))
            .OnBrain(async (systemPrompt, userPrompt) =>
                scriptedReplies.Count > 0 ? scriptedReplies.Dequeue() : "FINAL: done");
    }

    [Fact]
    public async Task ShouldNotRepeatAnEffectAfterTheRunWasInterruptedAsync()
    {
        // given — the first process pays the invoice and dies before it can answer
        var tool = new CountingTool();

        await NewProcess(tool, "ACTION: wire_transfer: 10000")
            .MaxTurns(1)
            .ProcessPromptAsync(prompt: "pay the invoice", "invoice-88", CancellationToken.None);

        tool.ExecutionCount.Should().Be(1);

        // when — a second process picks the session up and the Brain proposes the same act
        var resumedTool = new CountingTool();

        string actualResult = await NewProcess(
            resumedTool,
            "ACTION: wire_transfer: 10000",
            "FINAL: already paid")
                .ProcessPromptAsync(
                    prompt: "did that go through?", "invoice-88", CancellationToken.None);

        // then — the resumed run kept the interrupted run's identity, so the effect is replayed
        resumedTool.ExecutionCount.Should().Be(0);
        actualResult.Should().Be("already paid");
    }

    [Fact]
    public async Task ShouldRecordTheRunOnTheSessionBeforeAnyWorkAsync()
    {
        // given — a run cancelled before it starts, which never reaches the end of the loop and
        // is deliberately never written back as an answer
        var tool = new CountingTool();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await NewProcess(tool, "ACTION: wire_transfer: 10000")
            .ProcessPromptAsync(prompt: "pay the invoice", "invoice-91", cancellation.Token);

        // when
        var sessionBroker = new FileSessionBroker(SessionsPath);
        AgentSession? actualSession = await sessionBroker.SelectSessionAsync("invoice-91");

        // then — an identity recorded only on success is one the failure case can never use
        actualSession.Should().NotBeNull();
        actualSession!.RunId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldStartAFreshRunAfterTheSessionDeliveredAnAnswerAsync()
    {
        // given — the first prompt answers, which completes the run
        var tool = new CountingTool();

        await NewProcess(tool, "FINAL: nothing to do")
            .ProcessPromptAsync(prompt: "anything pending?", "invoice-93", CancellationToken.None);

        var sessionBroker = new FileSessionBroker(SessionsPath);
        AgentSession? answered = await sessionBroker.SelectSessionAsync("invoice-93");

        // when — the next prompt in the same conversation performs an act
        var payingTool = new CountingTool();

        await NewProcess(payingTool, "ACTION: wire_transfer: 10000", "FINAL: paid")
            .ProcessPromptAsync(prompt: "pay the invoice", "invoice-93", CancellationToken.None);

        AgentSession? afterPayment = await sessionBroker.SelectSessionAsync("invoice-93");

        // then — a completed conversation is not an interrupted run; the act is performed
        payingTool.ExecutionCount.Should().Be(1);
        afterPayment!.RunId.Should().NotBe(answered!.RunId);
    }
}
