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
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// Narration (SPEC.md §6.0): the agent says what it is doing, in the user's language, on a typed
// channel of its own. The prose is model-authored (a SAY: line) or tool-declared (a template
// floor), and either way it is user-visible output — so it crosses to the stream only after the
// Gate has screened it (Invariant 5: output MUST NOT cross a boundary un-vetted).
public class NarrationTests
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
        private readonly string output;

        public ScriptedTool(string name, string output)
        {
            Name = name;
            this.output = output;
        }

        public string Name { get; }
        public string Description => "A scripted tool.";
        public int Executions { get; private set; }

        public ValueTask<string> ExecuteAsync(string input)
        {
            Executions++;

            return ValueTask.FromResult(this.output);
        }
    }

    private static async ValueTask<List<AgentStreamEvent>> DrainAsync(
        StandardAgent agent, string prompt)
    {
        List<AgentStreamEvent> events = [];

        await foreach (AgentStreamEvent streamEvent in agent.StreamPromptAsync(prompt))
        {
            events.Add(streamEvent);
        }

        return events;
    }

    [Fact]
    public async Task ShouldScreenAndEmitModelNarrationOnStreamAsync()
    {
        // given
        var tool = new ScriptedTool("calculator", "4");
        int calls = 0;
        List<string> screened = [];

        StandardAgent agent = BareAgent((_, _) => ValueTask.FromResult(++calls == 1
            ? "SAY: Checking the calculator...\nACTION: calculator: 2+2"
            : "FINAL: 4"))
            .Tool(tool)
            .OnGate((_, text) =>
            {
                screened.Add(text);

                return ValueTask.FromResult("allow");
            });

        // when
        List<AgentStreamEvent> events = await DrainAsync(agent, "what is 2+2?");

        // then — the narration is voiced before the act it announces, and the gate saw it first
        events.Should().Contain(streamEvent =>
            streamEvent.Type == AgentStreamEventType.Narration
                && streamEvent.Content == "Checking the calculator...");

        int narrationIndex = events.FindIndex(streamEvent =>
            streamEvent.Type == AgentStreamEventType.Narration);

        int toolIndex = events.FindIndex(streamEvent =>
            streamEvent.Type == AgentStreamEventType.Tool);

        toolIndex.Should().BeGreaterThan(narrationIndex);

        string answer = string.Concat(events
            .Where(streamEvent => streamEvent.Type == AgentStreamEventType.Response)
            .Select(streamEvent => streamEvent.Content));

        answer.Should().Be("4");
        screened.Should().Contain("Checking the calculator...");
    }
}
