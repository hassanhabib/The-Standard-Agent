// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Skills;
using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Tools;

// Cancellation across the nesting seam. The how-to promises a cancelled run stops at the next
// turn boundary; a sub-agent runs its own loop with its own turn boundaries, so the outer
// token must reach it — otherwise cancelling the outer run leaves the inner one running to
// its own cap, doing work nobody wants anymore.
public partial class AgentToolTests
{
    private sealed class CancellingTool : ITool
    {
        private readonly CancellationTokenSource outerRun;

        public CancellingTool(CancellationTokenSource outerRun) =>
            this.outerRun = outerRun;

        public string Name => "calculator";
        public string Description => "Evaluates arithmetic.";
        public string Parameters => "{}";

        public int ExecutionCount { get; private set; }

        public ValueTask<string> ExecuteAsync(string input)
        {
            ExecutionCount++;
            this.outerRun.Cancel();

            return ValueTask.FromResult("4183");
        }
    }

    private static StandardAgent BareLoopingAgent(string brainReply)
    {
        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(EmptyKnowledgeBroker())
            .OnBrain(async (_, _) => brainReply);
    }

    [Fact]
    public async Task ShouldStopTheNestedRunWhenTheOuterRunIsCancelledAsync()
    {
        // given — an inner agent whose brain never terminates and proposes a DISTINCT act
        // each turn (an identical act would be spared by run-once and mask the leak), whose
        // tool cancels the OUTER run on its first execution, and enough inner turns that an
        // unstopped inner run is unmistakable in the count.
        using var outerRun = new CancellationTokenSource();
        var tool = new CancellingTool(outerRun);
        int innerThoughts = 0;

        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        StandardAgent inner = new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(EmptyKnowledgeBroker())
            .OnBrain(async (_, _) => $"ACTION: calculator: {++innerThoughts}")
            .Tool(tool)
            .MaxTurns(50);

        StandardAgent outer = BareLoopingAgent("ACTION: sub: do the work")
            .Tool(new AgentTool(name: "sub", agent: inner))
            .MaxTurns(2);

        // when — the outer run is cancelled while the inner one is on its first turn
        await outer.ProcessPromptAsync("delegate this", string.Empty, outerRun.Token);

        // then — the inner loop observed the outer token at its next turn boundary. Without
        // the token crossing the seam it runs all 50 turns for a run nobody wants.
        tool.ExecutionCount.Should().Be(
            1,
            because: "cancelling the outer run must stop the nested run at its next turn "
                + "boundary, not leave it working to its own cap");
    }
}
