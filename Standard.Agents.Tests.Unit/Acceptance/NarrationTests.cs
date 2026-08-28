// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Brokers.Audits;
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

    // A refused narration is withheld silently and recorded loudly: the user gains nothing from
    // "a progress note was withheld", and echoing the refusal would hand an injected SAY payload
    // a visible oracle — but the decision log carries the fact, which is where a review looks
    // (SPEC.md §4.7). The run itself is unharmed: narration is decoration, never the work.
    [Fact]
    public async Task ShouldWithholdRefusedNarrationAndRecordItOnStreamAsync()
    {
        // given
        var tool = new ScriptedTool("calculator", "4");
        int calls = 0;
        List<AuditRecord> trace = [];

        StandardAgent agent = BareAgent((_, _) => ValueTask.FromResult(++calls == 1
            ? "SAY: Checking the calculator...\nACTION: calculator: 2+2"
            : "FINAL: 4"))
            .Tool(tool)
            .OnGate((_, text) => ValueTask.FromResult(
                text.Contains("Checking the calculator")
                    ? "refuse: prompt injection"
                    : "allow"))
            .OnAudit(record =>
            {
                lock (trace)
                {
                    trace.Add(record);
                }

                return ValueTask.CompletedTask;
            });

        // when
        List<AgentStreamEvent> events = await DrainAsync(agent, "what is 2+2?");

        // then — nothing voiced, the run unharmed, the record carrying the fact
        events.Should().NotContain(streamEvent =>
            streamEvent.Content.Contains("Checking the calculator"));

        string answer = string.Concat(events
            .Where(streamEvent => streamEvent.Type == AgentStreamEventType.Response)
            .Select(streamEvent => streamEvent.Content));

        answer.Should().Be("4");

        trace.Should().Contain(record =>
            record.Message.Contains("Narration") && record.Message.Contains("WITHHELD"));
    }

    private sealed class NarratingTool : ITool
    {
        public string Name => "calculator";
        public string Description => "A scripted tool.";
        public string NarrationStarting => "Searching with {tool} for {payload}...";
        public string NarrationObserved => "Got results from {tool}.";

        public ValueTask<string> ExecuteAsync(string input) =>
            ValueTask.FromResult("4");
    }

    // The floor: a tool that declared its narration is voiced even when the model said nothing —
    // the run never goes silent just because the model was terse.
    [Fact]
    public async Task ShouldVoiceToolNarrationFloorWhenModelSaysNothingOnStreamAsync()
    {
        // given
        int calls = 0;

        StandardAgent agent = BareAgent((_, _) => ValueTask.FromResult(++calls == 1
            ? "ACTION: calculator: 2+2"
            : "FINAL: 4"))
            .Tool(new NarratingTool());

        // when
        List<AgentStreamEvent> events = await DrainAsync(agent, "what is 2+2?");

        // then — both slots voiced, interpolated, and in the story's order: the announcement,
        // the observation, then the Tool event carrying the data
        int startingIndex = events.FindIndex(streamEvent =>
            streamEvent.Type == AgentStreamEventType.Narration
                && streamEvent.Content == "Searching with calculator for 2+2...");

        int observedIndex = events.FindIndex(streamEvent =>
            streamEvent.Type == AgentStreamEventType.Narration
                && streamEvent.Content == "Got results from calculator.");

        int toolIndex = events.FindIndex(streamEvent =>
            streamEvent.Type == AgentStreamEventType.Tool);

        startingIndex.Should().BeGreaterThan(-1);
        observedIndex.Should().BeGreaterThan(startingIndex);
        toolIndex.Should().BeGreaterThan(observedIndex);
    }

    // Model narration beats the template for the pre-act slot: the model's grounded prose is
    // the ceiling, the tool's template only the floor. The observed slot is never overridden —
    // a SAY line speaks for the act, not for its outcome.
    [Fact]
    public async Task ShouldPreferModelNarrationOverToolTemplateOnStreamAsync()
    {
        // given
        int calls = 0;

        StandardAgent agent = BareAgent((_, _) => ValueTask.FromResult(++calls == 1
            ? "SAY: Let me check the calculator...\nACTION: calculator: 2+2"
            : "FINAL: 4"))
            .Tool(new NarratingTool());

        // when
        List<AgentStreamEvent> events = await DrainAsync(agent, "what is 2+2?");

        // then
        events.Should().Contain(streamEvent =>
            streamEvent.Type == AgentStreamEventType.Narration
                && streamEvent.Content == "Let me check the calculator...");

        events.Should().NotContain(streamEvent =>
            streamEvent.Content.Contains("Searching with"));

        events.Should().Contain(streamEvent =>
            streamEvent.Type == AgentStreamEventType.Narration
                && streamEvent.Content == "Got results from calculator.");
    }

    // Narration dies with its turn: it never enters the answer, the observations, or the
    // session's history — the next turn's brain input and the saved conversation carry none
    // of it (SPEC.md §4.11).
    [Fact]
    public async Task ShouldKeepNarrationOutOfTheSessionAndTheNextBrainInputAsync()
    {
        // given
        var tool = new ScriptedTool("calculator", "4");
        int calls = 0;
        List<string> brainInputs = [];
        List<Models.Brokers.Sessions.AgentSession> savedSessions = [];

        var sessionBroker = new Mock<Brokers.Sessions.ISessionBroker>();

        sessionBroker.Setup(broker => broker.SelectSessionAsync(It.IsAny<string>()))
            .ReturnsAsync((Models.Brokers.Sessions.AgentSession?)null);

        sessionBroker.Setup(broker =>
            broker.UpsertSessionAsync(It.IsAny<Models.Brokers.Sessions.AgentSession>()))
                .Callback((Models.Brokers.Sessions.AgentSession session) =>
                {
                    lock (savedSessions)
                    {
                        savedSessions.Add(session);
                    }
                })
                .Returns(ValueTask.CompletedTask);

        StandardAgent agent = BareAgent((_, userMessage) =>
        {
            lock (brainInputs)
            {
                brainInputs.Add(userMessage);
            }

            return ValueTask.FromResult(++calls == 1
                ? "SAY: Checking the calculator...\nACTION: calculator: 2+2"
                : "FINAL: 4");
        })
            .Tool(tool)
            .UseSessions(sessionBroker.Object);

        var request = new PromptRequest { Prompt = "what is 2+2?", SessionId = "session-1" };

        // when
        await foreach (AgentStreamEvent _ in agent.StreamPromptAsync(request))
        {
        }

        // then
        brainInputs.Should().OnlyContain(input =>
            input.Contains("Checking the calculator") == false);

        savedSessions.Should().OnlyContain(session =>
            session.History.All(turn =>
                turn.Prompt.Contains("Checking the calculator") == false
                    && turn.Answer.Contains("Checking the calculator") == false));
    }
}
