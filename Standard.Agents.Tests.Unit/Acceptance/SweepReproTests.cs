// ---------------------------------------------------------------
// Review-sweep demonstrations (2026-08-23). Every test asserts the
// behaviour the spec or docs promise; each is Skipped because it
// FAILS against the current code — remove a Skip to reproduce its
// finding. See docs/reviews/2026-08-23-full-sweep.md. Delete this
// file as each finding is fixed and replaced by a real test.
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Foundations.Skills;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Models.Orchestrations.Effects;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

public class SweepReproTests : IDisposable
{
    private readonly string sessionsPath =
        Path.Combine(Path.GetTempPath(), $"standard-agent-sweep-{Guid.NewGuid():n}");

    public void Dispose()
    {
        if (Directory.Exists(this.sessionsPath))
        {
            Directory.Delete(this.sessionsPath, recursive: true);
        }
    }

    private sealed class CountingTool : ITool
    {
        private readonly string result;

        public CountingTool(string result = "4183") =>
            this.result = result;

        public string Name => "calculator";
        public string Description => "Evaluates arithmetic.";
        public string Parameters => "{}";

        public int ExecutionCount { get; private set; }

        public ValueTask<string> ExecuteAsync(string input)
        {
            ExecutionCount++;

            return ValueTask.FromResult(this.result);
        }
    }

    private static StandardAgent BareAgent(
        Func<string, string, ValueTask<string>> brain)
    {
        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .OnBrain(brain);
    }

    private static async Task<List<AgentStreamEvent>> DrainAsync(
        IAsyncEnumerable<AgentStreamEvent> events)
    {
        List<AgentStreamEvent> drained = [];

        await foreach (AgentStreamEvent streamEvent in events)
        {
            drained.Add(streamEvent);
        }

        return drained;
    }

    // FINDING 7 — a turn-capped run records the last tool output in the conversation
    // as though it were the agent's answer. Skipped through 1.6.1 as the open demonstration;
    // resolved by decision: a capped run stops without delivering an answer, like a budget stop.
    [Fact]
    public async Task Finding7_TurnCappedRunMustNotRecordAnAnswerItNeverGaveAsync()
    {
        var tool = new CountingTool();

        StandardAgent capped = BareAgent((_, _) =>
            ValueTask.FromResult("ACTION: calculator: 47*89"))
            .Tool(tool)
            .Sessions(this.sessionsPath)
            .MaxTurns(2);

        await capped.ProcessPromptAsync("loop", "conv-1", CancellationToken.None);

        string followUpPrompt = string.Empty;

        StandardAgent follower = BareAgent((_, userPrompt) =>
        {
            followUpPrompt = userPrompt;

            return ValueTask.FromResult("FINAL: hello");
        })
            .Sessions(this.sessionsPath);

        await follower.ProcessPromptAsync("hi again", "conv-1", CancellationToken.None);

        followUpPrompt.Should().NotContain(
            "4183",
            because: "the capped run never delivered an answer, and the next prompt must not "
                + "be told the agent said something it never said");
    }

    // FINDING 7b — documents the actual outcome shape at the cap (expected to PASS;
    // evidence for the AgentOutcome doc-comment mismatch, not a failure).
    [Fact]
    public async Task Finding7b_TurnCapReturnsLastToolResultWithStatusWorkingAsync()
    {
        var tool = new CountingTool();

        StandardAgent capped = BareAgent((_, _) =>
            ValueTask.FromResult("ACTION: calculator: 47*89"))
            .Tool(tool)
            .MaxTurns(2);

        AgentOutcome outcome = await capped.RunAsync("loop");

        outcome.Result.Should().Be("4183");
        outcome.Status.Should().Be(AgentStatus.Working);
    }
}
