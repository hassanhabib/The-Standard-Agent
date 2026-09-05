// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Effects;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Brokers.Effects;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Foundations.Skills;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// Found in the 2026-09-04 principal review (F-08): the ledger claimed an act before it ran and
// recorded its outcome after, and the window between the two was a string. A process that died,
// or a store that failed, after the transfer went out but before the outcome was written left a
// claim with no outcome — and every later proposal of the same act read "effect already in
// progress" as though it were the act's result, forever. The ledger now keeps typed states with
// an owner and a lease; an earlier attempt with no usable outcome HOLDS the run with the act as
// its pending effect, so a person can reconcile it against the world, and a reconciled record is
// replayed the way any completed act is.
public class EffectLedgerReconciliationTests : IDisposable
{
    private const string SessionId = "invoice-4471";
    private const string Proposal = "ACTION: wire_transfer: 10000";

    private readonly string sessionsPath =
        Path.Combine(Path.GetTempPath(), $"standard-agent-reconcile-{Guid.NewGuid():n}");

    public void Dispose()
    {
        if (Directory.Exists(this.sessionsPath))
        {
            Directory.Delete(this.sessionsPath, recursive: true);
        }
    }

    private sealed class CountingTool : ITool
    {
        private readonly Exception? failure;

        public CountingTool(Exception? failure = null) =>
            this.failure = failure;

        public string Name => "wire_transfer";

        public string Description => "Moves money.";

        public int ExecutionCount { get; private set; }

        public async ValueTask<string> ExecuteAsync(string input)
        {
            ExecutionCount++;

            return this.failure is null ? "transfer complete" : throw this.failure;
        }
    }

    // The act happens; the note that it happened does not. A ledger whose first outcome write
    // fails, over a store that otherwise works — the consistency window, made deterministic.
    private sealed class FailingOutcomeLedger : IEffectLedgerBroker
    {
        private readonly InMemoryEffectLedgerBroker store = new();
        private bool failedOnce;

        public ValueTask<bool> InsertClaimAsync(EffectRecord claim) =>
            this.store.InsertClaimAsync(claim);

        public ValueTask<EffectRecord?> SelectRecordAsync(string idempotencyKey) =>
            this.store.SelectRecordAsync(idempotencyKey);

        public ValueTask UpdateRecordAsync(EffectRecord record)
        {
            if (record.State is EffectState.Completed && this.failedOnce is false)
            {
                this.failedOnce = true;

                throw new IOException("the ledger store is unavailable");
            }

            return this.store.UpdateRecordAsync(record);
        }

        public ValueTask DeleteRecordAsync(string idempotencyKey) =>
            this.store.DeleteRecordAsync(idempotencyKey);
    }

    // A separate instance each time over the same session folder and the same ledger — the
    // closest a unit test comes to a different process.
    private StandardAgent NewProcess(
        IEffectLedgerBroker ledger,
        CountingTool tool,
        params string[] replies)
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
            .Sessions(this.sessionsPath)
            .UseEffectLedger(ledger)
            .OnBrain(async (systemPrompt, userPrompt) =>
                scriptedReplies.Count > 0 ? scriptedReplies.Dequeue() : "FINAL: done");
    }

    private static PromptRequest Request(string prompt) =>
        new() { Prompt = prompt, SessionId = SessionId };

    [Fact]
    public async Task ShouldHoldARepeatedActWhoseEarlierAttemptHasNoRecordedOutcomeAsync()
    {
        // given — the first process performs the transfer and the ledger loses the outcome
        var ledger = new FailingOutcomeLedger();
        var tool = new CountingTool();

        StandardAgent firstProcess = NewProcess(ledger, tool, Proposal).MaxTurns(1);

        Func<Task> firstRun = async () =>
            await firstProcess.RunAsync(Request("pay the invoice"), CancellationToken.None);

        await firstRun.Should().ThrowAsync<Exception>();
        tool.ExecutionCount.Should().Be(1);

        // when — a second process picks the session up and the Brain proposes the same act
        var resumedTool = new CountingTool();

        AgentOutcome actualOutcome = await NewProcess(ledger, resumedTool, Proposal, "FINAL: paid")
            .RunAsync(Request("did that go through?"), CancellationToken.None);

        // then — not performed again, not replayed as if it had an outcome: held, with the act
        // on the outcome so a person can reconcile it against the world
        resumedTool.ExecutionCount.Should().Be(0);
        actualOutcome.Status.Should().Be(AgentStatus.AwaitingInput);
        actualOutcome.Result.Should().Contain("reconcile");
        actualOutcome.PendingEffect.Should().NotBeNull();
        actualOutcome.PendingEffect!.ToolName.Should().Be("wire_transfer");
        actualOutcome.PendingEffect.IdempotencyKey.Should().NotBeEmpty();

        EffectRecord? actualRecord =
            await ledger.SelectRecordAsync(actualOutcome.PendingEffect.IdempotencyKey);

        actualRecord.Should().NotBeNull();
        actualRecord!.State.Should().Be(EffectState.InFlight);
        actualRecord.ToolName.Should().Be("wire_transfer");
    }

    [Fact]
    public async Task ShouldReplayTheActOnceTheLedgerIsReconciledAsync()
    {
        // given — the same interrupted attempt, then a person reconciles the record
        var ledger = new FailingOutcomeLedger();
        var tool = new CountingTool();
        StandardAgent firstProcess = NewProcess(ledger, tool, Proposal).MaxTurns(1);

        Func<Task> firstRun = async () =>
            await firstProcess.RunAsync(Request("pay the invoice"), CancellationToken.None);

        await firstRun.Should().ThrowAsync<Exception>();

        AgentOutcome heldOutcome = await NewProcess(ledger, new CountingTool(), Proposal, "FINAL: paid")
            .RunAsync(Request("did that go through?"), CancellationToken.None);

        string idempotencyKey = heldOutcome.PendingEffect!.IdempotencyKey;
        EffectRecord unreconciled = (await ledger.SelectRecordAsync(idempotencyKey))!;

        await ledger.UpdateRecordAsync(unreconciled with
        {
            State = EffectState.Completed,
            Outcome = "transfer complete"
        });

        // when — a third process, same session, same proposal
        var thirdTool = new CountingTool();

        AgentOutcome actualOutcome =
            await NewProcess(ledger, thirdTool, Proposal, "FINAL: already paid")
                .RunAsync(Request("and now?"), CancellationToken.None);

        // then — replayed from the reconciled record, never performed again
        thirdTool.ExecutionCount.Should().Be(0);
        tool.ExecutionCount.Should().Be(1);
        actualOutcome.Status.Should().Be(AgentStatus.Responded);
        actualOutcome.Result.Should().Be("already paid");
    }

    [Fact]
    public async Task ShouldRecordAFailedAttemptAndHoldItsRepeatAsync()
    {
        // given — the tool itself fails, so whether the act happened is unknown
        var ledger = new InMemoryEffectLedgerBroker();
        var failingTool = new CountingTool(new InvalidOperationException("the bank is offline"));
        StandardAgent firstProcess = NewProcess(ledger, failingTool, Proposal).MaxTurns(1);

        Func<Task> firstRun = async () =>
            await firstProcess.RunAsync(Request("pay the invoice"), CancellationToken.None);

        await firstRun.Should().ThrowAsync<Exception>();
        failingTool.ExecutionCount.Should().Be(1);

        // when — the bank is back and the Brain proposes the same act on the same session
        var healthyTool = new CountingTool();

        AgentOutcome actualOutcome = await NewProcess(ledger, healthyTool, Proposal, "FINAL: paid")
            .RunAsync(Request("try again"), CancellationToken.None);

        // then — the failed attempt is on the record as failed, and the repeat is held, not run
        healthyTool.ExecutionCount.Should().Be(0);
        actualOutcome.Status.Should().Be(AgentStatus.AwaitingInput);

        EffectRecord? actualRecord =
            await ledger.SelectRecordAsync(actualOutcome.PendingEffect!.IdempotencyKey);

        actualRecord!.State.Should().Be(EffectState.Failed);
    }
}
