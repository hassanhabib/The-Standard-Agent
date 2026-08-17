// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Foundations.Skills;
using Standard.Agents.Models.Orchestrations.Effects;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// The perimeter end to end (SPEC.md §4.9): authorize, record intent, approve, run at most once,
// record outcome. These pin the guarantees a bank actually asks about.
public class PerimeterTests
{
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

    private static StandardAgent AgentThatCallsTheTool(CountingTool tool, params string[] replies)
    {
        var skillBroker = new Mock<ISkillBroker>();

        skillBroker.Setup(broker => broker.SelectSkillsAsync())
            .ReturnsAsync(new List<Skill>());

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
            .OnBrain(async (systemPrompt, userPrompt) =>
                scriptedReplies.Count > 0 ? scriptedReplies.Dequeue() : "FINAL: done");
    }

    [Fact]
    public async Task ShouldNotRunAnIrreversibleToolWithoutApprovalAsync()
    {
        // given
        var tool = new CountingTool();

        StandardAgent agent =
            AgentThatCallsTheTool(tool, "ACTION: wire_transfer: 10000")
                .RequireApproval("wire_transfer");

        // when
        string actualResult = await agent.ProcessPromptAsync(prompt: "pay the invoice");

        // then — waiting is not consent
        tool.ExecutionCount.Should().Be(0);
        actualResult.Should().Contain("waiting for approval");
    }

    [Fact]
    public async Task ShouldRunAnIrreversibleToolOnceApprovedAsync()
    {
        // given
        var tool = new CountingTool();

        StandardAgent agent =
            AgentThatCallsTheTool(tool, "ACTION: wire_transfer: 10000", "FINAL: paid")
                .RequireApproval("wire_transfer")
                .OnApproval(effect => ValueTask.FromResult(ApprovalDecision.Approved));

        // when
        string actualResult = await agent.ProcessPromptAsync(prompt: "pay the invoice");

        // then
        tool.ExecutionCount.Should().Be(1);
        actualResult.Should().Be("paid");
    }

    [Fact]
    public async Task ShouldRunTheSameEffectOnlyOnceWithinARunAsync()
    {
        // given — the Brain proposes the identical transfer three times, as a looping model does
        var tool = new CountingTool();

        StandardAgent agent = AgentThatCallsTheTool(
            tool,
            "ACTION: wire_transfer: 10000",
            "ACTION: wire_transfer: 10000",
            "ACTION: wire_transfer:  10000",
            "FINAL: paid");

        // when
        await agent.ProcessPromptAsync(prompt: "pay the invoice");

        // then — one act, however many times it is proposed
        tool.ExecutionCount.Should().Be(1);
    }

    [Fact]
    public async Task ShouldDenyAnUnauthorizedEffectWithoutEndingTheRunAsync()
    {
        // given
        var tool = new CountingTool();

        StandardAgent agent = AgentThatCallsTheTool(
            tool,
            "ACTION: wire_transfer: 10000",
            "FINAL: I could not do that")
                .OnPolicy(effect => ValueTask.FromResult(
                    AuthorizationDecision.Deny("wire transfers are not permitted for this user")));

        // when
        string actualResult = await agent.ProcessPromptAsync(prompt: "pay the invoice");

        // then — denial is non-terminal: the agent is told, and answers anyway
        tool.ExecutionCount.Should().Be(0);
        actualResult.Should().Be("I could not do that");
    }
}
