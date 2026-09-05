// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Brokers.Generators.V1;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Foundations.Skills;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// SPEC.md §4.6, end to end through the real composition.
//
// These used to be four unit tests, one per service, back when each of Brain, Gate and Judge held
// its own redaction broker. Redaction is now a decoration applied at composition, so the failure
// worth guarding against changed: not "a service forgot to redact" but "a broker was left
// unwrapped". Only a test that drives the whole composition can see that, which is why these
// moved up rather than moving sideways onto the decorators.
public class RedactionTests
{
    private const string SecretEmail = "jane@acme.com";

    private static StandardAgent AgentThatRedacts(
        Func<string, string, ValueTask<string>> brain,
        Func<string, string, ValueTask<string>>? gate = null,
        Func<string, string, ValueTask<string>>? judge = null)
    {
        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        StandardAgent agent = new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .OnBrain(brain)
            .Redact();

        if (gate is not null)
        {
            agent = agent.OnGate(gate);
        }

        if (judge is not null)
        {
            agent = agent.OnJudge(judge);
        }

        return agent;
    }

    [Fact]
    public async Task ShouldHideTheValueFromTheBrainAndGiveItBackToTheCallerAsync()
    {
        // given
        string seenByBrain = string.Empty;

        StandardAgent agent = AgentThatRedacts(async (systemPrompt, userPrompt) =>
        {
            seenByBrain = userPrompt;

            return "FINAL: I emailed {{EMAIL_0}} as requested.";
        });

        // when
        string actualAnswer =
            await agent.ProcessPromptAsync($"please email {SecretEmail} the report");

        // then
        seenByBrain.Should().NotContain(SecretEmail);
        seenByBrain.Should().Contain("{{EMAIL_0}}");
        actualAnswer.Should().Be($"I emailed {SecretEmail} as requested.");
    }

    // The Gate screens the raw task, so it sees exactly what redaction exists to hide — and a
    // guardian may run on a different host than the Brain, which makes an unredacted Gate a
    // wider exposure than an unredacted Brain, not a narrower one.
    [Fact]
    public async Task ShouldHideTheValueFromTheGateAsync()
    {
        // given
        string seenByGate = string.Empty;

        StandardAgent agent = AgentThatRedacts(
            brain: async (systemPrompt, userPrompt) => "FINAL: done",
            gate: async (rubric, input) =>
            {
                seenByGate = input;

                return "accept";
            });

        // when
        await agent.ProcessPromptAsync($"please email {SecretEmail} the report");

        // then
        seenByGate.Should().NotBeEmpty();
        seenByGate.Should().NotContain(SecretEmail);
    }

    // The Judge reads the task AND the drafted answer, so it sees more sensitive text than any
    // other guardian — and it is the one most likely to be pointed at a cheap third-party
    // endpoint, because scoring is the cheapest of the three calls.
    [Fact]
    public async Task ShouldHideTheValueFromTheJudgeAsync()
    {
        // given
        string seenByJudge = string.Empty;

        StandardAgent agent = AgentThatRedacts(
            brain: async (systemPrompt, userPrompt) => $"FINAL: sent to {SecretEmail}",
            judge: async (rubric, input) =>
            {
                seenByJudge = input;

                return "1.0";
            });

        // when
        await agent.ProcessPromptAsync($"please email {SecretEmail} the report");

        // then
        seenByJudge.Should().NotBeEmpty();
        seenByJudge.Should().NotContain(SecretEmail);
    }

    // A placeholder can arrive split across two chunks. Without the streaming buffer the caller
    // sees "{{EMAIL_" and never gets the value back.
    private sealed class SplittingGeneratorBroker : IGeneratorBroker
    {
        public string SeenUserPrompt { get; private set; } = string.Empty;

        public ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt)
        {
            SeenUserPrompt = userPrompt;

            return ValueTask.FromResult("FINAL: done");
        }

        public async IAsyncEnumerable<string> GenerateStreamAsync(
            string systemPrompt,
            string userPrompt,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            SeenUserPrompt = userPrompt;

            await ValueTask.CompletedTask;

            yield return "FINAL: sent to {{EMAI";
            yield return "L_0}} just now";
        }
    }

    [Fact]
    public async Task ShouldRehydrateAStreamedValueSplitAcrossChunksAsync()
    {
        // given
        var generator = new SplittingGeneratorBroker();

        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        StandardAgent agent = new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .UseGenerator(generator)
            .Redact();

        // when
        var streamed = new List<string>();

        await foreach (AgentStreamEvent streamEvent in
            agent.StreamPromptAsync($"please email {SecretEmail} the report"))
        {
            streamed.Add(streamEvent.Content);
        }

        // then
        generator.SeenUserPrompt.Should().NotContain(SecretEmail);

        string whole = string.Concat(streamed);
        whole.Should().Contain(SecretEmail);
        whole.Should().NotContain("{{EMAI");
    }

    [Fact]
    public async Task ShouldNotRedactWhenRedactionIsNotConfiguredAsync()
    {
        // given — redaction is opt-in; absent it the agent behaves as if §4.6 did not exist
        string seenByBrain = string.Empty;

        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        StandardAgent agent = new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .OnBrain(async (systemPrompt, userPrompt) =>
            {
                seenByBrain = userPrompt;

                return "FINAL: done";
            });

        // when
        await agent.ProcessPromptAsync($"please email {SecretEmail} the report");

        // then
        seenByBrain.Should().Contain(SecretEmail);
    }

    // The 2026-08-23 sweep found Redaction was a capability with one mode: .Redact() hardcoded
    // the default rules, and there was no way to supply your own rules, broker, or delegates.
    // These pin the new modes BEHAVIOURALLY — a name-existence check passes without any wiring,
    // which is exactly how a seam gets a method and no plumbing.
    [Fact]
    public async Task ShouldRedactAtTheWireWithCustomDelegatesAsync()
    {
        // given — the Custom mode: a codename scheme no regex ships with
        string seenByBrain = string.Empty;

        StandardAgent agent = AgentBare((_, userPrompt) =>
        {
            seenByBrain = userPrompt;

            return ValueTask.FromResult("FINAL: I paged {{ONCALL}} already.");
        })
            .OnRedaction(
                redact: (text, vault) =>
                {
                    if (text.Contains("Jane Doe"))
                    {
                        vault["{{ONCALL}}"] = "Jane Doe";

                        return text.Replace("Jane Doe", "{{ONCALL}}");
                    }

                    return text;
                },
                rehydrate: (text, vault) =>
                    vault.TryGetValue("{{ONCALL}}", out string? name)
                        ? text.Replace("{{ONCALL}}", name)
                        : text);

        // when
        string actualAnswer = await agent.ProcessPromptAsync("page Jane Doe about the outage");

        // then — the model never saw the name, and the caller got it back
        seenByBrain.Should().NotContain("Jane Doe");
        seenByBrain.Should().Contain("{{ONCALL}}");
        actualAnswer.Should().Be("I paged Jane Doe already.");
    }

    [Fact]
    public async Task ShouldRedactByTheHostsOwnRulesAsync()
    {
        // given — the Local mode with custom rules: an internal ticket format
        string seenByBrain = string.Empty;

        StandardAgent agent = AgentBare((_, userPrompt) =>
        {
            seenByBrain = userPrompt;

            return ValueTask.FromResult("FINAL: filed");
        })
            .Redact(new Models.Foundations.Brains.RedactionRule
            {
                Label = "TICKET",
                Pattern = @"INC-\d{6}"
            });

        // when
        await agent.ProcessPromptAsync("escalate INC-004512 to the vendor");

        // then
        seenByBrain.Should().NotContain("INC-004512");
        seenByBrain.Should().Contain("{{TICKET_0}}");
    }

    private sealed class SendEmailTool : ITool
    {
        public string Name => "send_email";
        public string Description => "Sends an email.";
        public string ReceivedInput { get; private set; } = string.Empty;

        public async ValueTask<string> ExecuteAsync(string input)
        {
            ReceivedInput = input;

            return "sent";
        }
    }

    // Found in the 2026-09-04 principal review (F-02): the native decorator redacted each
    // message's Content and rehydrated the tool-call arguments coming BACK, but never redacted
    // the arguments going OUT. A value the model echoed into an argument on turn one was
    // rehydrated for the tool, recorded in the exchange, and replayed to the model in the clear
    // on turn two. §4.6 says every model call, and a replayed tool call is part of a model call.
    [Fact]
    public async Task ShouldHideTheValueFromTheNativeBrainOnReplayedToolCallArgumentsAsync()
    {
        // given
        var tool = new SendEmailTool();
        List<string> seenByBrainOnSecondCall = [];
        int brainCalls = 0;

        StandardAgent agent = AgentBareNative(
            tool,
            async (messages, tools) =>
            {
                brainCalls++;

                if (brainCalls == 1)
                {
                    return new GenerationResult
                    {
                        ToolCalls =
                        [
                            new ModelToolCall(
                                Id: "call_1",
                                Name: "send_email",
                                ArgumentsJson: """{"to":"{{EMAIL_0}}","subject":"report"}""")
                        ]
                    };
                }

                seenByBrainOnSecondCall =
                [
                    .. messages.Select(message => message.Content),
                    .. messages.SelectMany(message => message.ToolCalls)
                        .Select(toolCall => toolCall.ArgumentsJson)
                ];

                return new GenerationResult { Content = "Sent the report to {{EMAIL_0}}." };
            })
            .Redact();

        // when
        string actualAnswer =
            await agent.ProcessPromptAsync($"please email {SecretEmail} the report");

        // then — the tool got the real address, the model never did, the caller got it back
        tool.ReceivedInput.Should().Contain(SecretEmail);
        seenByBrainOnSecondCall.Should().NotBeEmpty();

        seenByBrainOnSecondCall.Should().NotContain(modelInput =>
            modelInput.Contains(SecretEmail));

        seenByBrainOnSecondCall.Should().Contain(modelInput =>
            modelInput.Contains("{{EMAIL_0}}") && modelInput.Contains("\"to\""));

        actualAnswer.Should().Be($"Sent the report to {SecretEmail}.");
    }

    private static StandardAgent AgentBareNative(
        ITool tool,
        Func<
            IReadOnlyList<ConversationMessage>,
            IReadOnlyList<ToolDefinition>,
            ValueTask<GenerationResult>> brain)
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
            .Tool(tool)
            .OnNativeBrain(brain);
    }

    private static StandardAgent AgentBare(Func<string, string, ValueTask<string>> brain)
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
}
