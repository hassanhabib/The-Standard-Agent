// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Brokers.Effects;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Sessions;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Brokers.Sessions;
using Standard.Agents.Models.Foundations.Skills;
using Standard.Agents.Models.Orchestrations.Agents;
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
    public async Task ShouldPerformAHeldActOnceTheAuthorityApprovesItAsync()
    {
        // given — the first process proposes the transfer and the authority has not answered yet
        var tool = new CountingTool();

        string held = await NewProcess(tool, "ACTION: wire_transfer: 10000")
            .RequireApproval("wire_transfer")
            .OnApproval(effect => ValueTask.FromResult(ApprovalDecision.Pending))
            .ProcessPromptAsync(prompt: "pay the invoice", "invoice-95", CancellationToken.None);

        held.Should().Contain("waiting for approval");
        tool.ExecutionCount.Should().Be(0);

        // when — the authority says yes, and a second process resumes the run
        var resumedTool = new CountingTool();

        string actualResult = await NewProcess(
            resumedTool,
            "ACTION: wire_transfer: 10000",
            "FINAL: paid")
                .RequireApproval("wire_transfer")
                .OnApproval(effect => ValueTask.FromResult(ApprovalDecision.Approved))
                .ProcessPromptAsync(
                    prompt: "yes, approve it", "invoice-95", CancellationToken.None);

        // then — a held act is one that still has to be able to happen; an approval that arrives
        // after the claim was left standing is an approval that can never be used
        resumedTool.ExecutionCount.Should().Be(1);
        actualResult.Should().Be("paid");
    }

    [Fact]
    public async Task ShouldFinishAHeldRunThroughResumeAsync()
    {
        // given — the authority has not answered, so the act is held
        var tool = new CountingTool();

        await NewProcess(tool, "ACTION: wire_transfer: 10000")
            .RequireApproval("wire_transfer")
            .OnApproval(effect => ValueTask.FromResult(ApprovalDecision.Pending))
            .ProcessPromptAsync(prompt: "pay the invoice", "invoice-97", CancellationToken.None);

        // when — a different process resumes the conversation with the authority's yes
        var resumedTool = new CountingTool();

        string actualResult = await NewProcess(
            resumedTool,
            "ACTION: wire_transfer: 10000",
            "FINAL: paid")
                .RequireApproval("wire_transfer")
                .OnApproval(effect => ValueTask.FromResult(ApprovalDecision.Approved))
                .ResumeAsync(sessionId: "invoice-97", answer: "yes, approve it");

        // then — resuming asks nothing of the caller but the answer; the session holds the rest
        resumedTool.ExecutionCount.Should().Be(1);
        actualResult.Should().Be("paid");
    }

    // SPEC.md §4.11 says a session carries `pending : AgentEffect?` — the act it was left waiting
    // on. Storing only the question text means a resuming process cannot show a human WHAT they
    // are approving, and an approval broker cannot check that the act it is now approving is the
    // act that was held.
    [Fact]
    public async Task ShouldRememberTheActTheSessionIsWaitingOnAsync()
    {
        // given
        var tool = new CountingTool();

        await NewProcess(tool, "ACTION: wire_transfer: 10000")
            .RequireApproval("wire_transfer")
            .OnApproval(effect => ValueTask.FromResult(ApprovalDecision.Pending))
            .ProcessPromptAsync(prompt: "pay the invoice", "invoice-99", CancellationToken.None);

        // when
        var sessionBroker = new FileSessionBroker(SessionsPath);
        AgentSession? actualSession = await sessionBroker.SelectSessionAsync("invoice-99");

        // then — the held act travels with the session, so the authority is shown the act and
        // not merely told that something is waiting
        actualSession.Should().NotBeNull();
        actualSession!.Status.Should().Be(AgentStatus.AwaitingApproval);
        actualSession.PendingEffect.Should().NotBeNull();
        actualSession.PendingEffect!.ToolName.Should().Be("wire_transfer");
        actualSession.PendingEffect.Arguments.Should().Contain("10000");
        actualSession.PendingEffect.IdempotencyKey.Should().NotBeEmpty();
        actualSession.PendingEffect.ApprovalRequired.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldRememberNoActWhenNothingIsHeldAsync()
    {
        // given — a run that simply answered
        var tool = new CountingTool();

        await NewProcess(tool, "FINAL: nothing to do")
            .ProcessPromptAsync(prompt: "anything pending?", "invoice-101", CancellationToken.None);

        // when
        var sessionBroker = new FileSessionBroker(SessionsPath);
        AgentSession? actualSession = await sessionBroker.SelectSessionAsync("invoice-101");

        // then — a session that is waiting on nothing says so
        actualSession!.PendingEffect.Should().BeNull();
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
