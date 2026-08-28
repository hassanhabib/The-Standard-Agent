// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Foundations.Skills;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// The streamed outcome (SPEC.md §4.14): one enumeration, every event live, and — once it
// completes — the SAME structured outcome the batched door returns. A caller never chooses
// between the answer's structure and the run's story.
public class StreamedOutcomeTests
{
    private static StandardAgent BareAgent(Func<string, string, ValueTask<string>> brain)
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

    private sealed class ScriptedTool : ITool
    {
        public string Name => "wire_transfer";
        public string Description => "A scripted tool.";
        public int Executions { get; private set; }

        public ValueTask<string> ExecuteAsync(string input)
        {
            Executions++;

            return ValueTask.FromResult("paid");
        }
    }

    [Fact]
    public async Task ShouldCarryTheHeldActOnTheStreamedOutcomeAsync()
    {
        // given — a run that ends held for approval: the batched door reports the hold and
        // the act itself; the streamed door must report exactly the same ending
        var tool = new ScriptedTool();

        StandardAgent agent = BareAgent((_, _) =>
            ValueTask.FromResult("ACTION: wire_transfer: 10"))
            .Tool(tool)
            .RequireApproval("wire_transfer");

        // when — through the contract, which is where a host calls it
        IAgent contract = agent;
        AgentRunStream runStream = contract.RunStreamAsync("pay the invoice");
        List<AgentStreamEvent> events = [];

        await foreach (AgentStreamEvent streamEvent in runStream)
        {
            events.Add(streamEvent);
        }

        // then — the events flowed AND the outcome is the batched door's, structure included
        events.Should().NotBeEmpty();
        tool.Executions.Should().Be(0);

        runStream.Outcome.Status.Should().Be(AgentStatus.AwaitingApproval);
        runStream.Outcome.PendingEffect.Should().NotBeNull();
        runStream.Outcome.PendingEffect!.ToolName.Should().Be("wire_transfer");
    }
}
