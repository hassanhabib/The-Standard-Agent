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
using Standard.Agents.Models.Brokers.Generators;
using Standard.Agents.Models.Brokers.Generators.V1;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Foundations.Skills;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// Per-request inference (docs/per-request-inference.md). One composed agent is configured once
// and asked many times; a request carries its own inference options, and precedence — configured
// → request → framework default — is resolved once, at the boundary. These pin the request seam:
// what a caller may shape, what the deployment always wins, and what degrades gracefully.
public class PerRequestInferenceTests
{
    private static StandardAgent AgentWith(Func<string, string, ValueTask<string>> brain)
    {
        var skillBroker = new Mock<ISkillBroker>();

        skillBroker.Setup(broker => broker.SelectSkillsAsync())
            .ReturnsAsync(new List<Skill> { new() { Content = "You are a helpful agent." } });

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

    // The request's schema seeds the guardian, not only the wire (design §4.1): plenty of
    // engines accept response_format and quietly ignore it, and this brain ignores everything —
    // so the only thing standing between the caller and a misshapen answer is the guardian
    // holding the draft to the requested shape and sending it back.
    [Fact]
    public async Task ShouldHoldTheAnswerToTheRequestSchemaWhenNoContractIsConfiguredAsync()
    {
        // given — a brain whose first draft breaks the requested shape and whose second matches
        int call = 0;

        StandardAgent agent = AgentWith((systemPrompt, userPrompt) =>
            ValueTask.FromResult(call++ == 0
                ? "FINAL: Paris"
                : """FINAL: {"city":"Paris"}"""));

        var request = new PromptRequest
        {
            Prompt = "capital of France, as JSON",
            ResponseSchemaJson = """{"type":"object","required":["city"]}"""
        };

        // when
        string actualResult = await agent.ProcessPromptAsync(request);

        // then — the malformed draft was revised against the schema the request asked for
        actualResult.Should().Be("""{"city":"Paris"}""");
    }

    // Configuration is a ceiling, not a suggestion (design §4). The first draft matches the
    // REQUEST's shape exactly — and is still revised, because a configured Contract discards the
    // request's schema outright. Never merged, never partially honored: the model is constrained
    // to one schema and validated against that same schema, which is what makes the
    // constrained-to-A-validated-against-B loop unreachable.
    [Fact]
    public async Task ShouldDiscardTheRequestSchemaWhenAContractIsConfiguredAsync()
    {
        // given — a configured Contract requiring "city", a request asking for "name"
        int call = 0;

        StandardAgent agent = AgentWith((systemPrompt, userPrompt) =>
            ValueTask.FromResult(call++ == 0
                ? """FINAL: {"name":"Paris"}"""
                : """FINAL: {"city":"Paris"}"""))
            .Contract("""{"type":"object","required":["city"]}""");

        var request = new PromptRequest
        {
            Prompt = "capital of France, as JSON",
            ResponseSchemaJson = """{"type":"object","required":["name"]}"""
        };

        // when
        string actualResult = await agent.ProcessPromptAsync(request);

        // then — configured won: the draft shaped like the request was sent back
        actualResult.Should().Be("""{"city":"Paris"}""");
    }

    // A broker that opts in — it implements the request-carrying overload and says so. What it
    // captures is the resolution's OUTPUT: a broker below the boundary writes what it is given
    // and decides nothing (design §4.2).
    private sealed class CapturingGeneratorBroker : IGeneratorBroker
    {
        public ResolvedInference? Captured { get; private set; }

        public bool HonorsRequest => true;

        public ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt) =>
            ValueTask.FromResult("FINAL: ok");

        public ValueTask<string> GenerateAsync(
            string systemPrompt,
            string userPrompt,
            ResolvedInference inference)
        {
            this.Captured = inference;

            return ValueTask.FromResult("FINAL: ok");
        }

        public async IAsyncEnumerable<string> GenerateStreamAsync(
            string systemPrompt,
            string userPrompt,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return "FINAL: ok";
        }

        public async IAsyncEnumerable<string> GenerateStreamAsync(
            string systemPrompt,
            string userPrompt,
            ResolvedInference inference,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            this.Captured = inference;

            yield return "FINAL: ok";
        }
    }

    private static StandardAgent AgentWith(CapturingGeneratorBroker broker)
    {
        var skillBroker = new Mock<ISkillBroker>();

        skillBroker.Setup(b => b.SelectSkillsAsync())
            .ReturnsAsync(new List<Skill> { new() { Content = "You are a helpful agent." } });

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(b => b.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(b => b.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .UseGenerator(broker);
    }

    // §4.2's exact blocker, pinned: the host called .Brain and said nothing about temperature.
    // "Configured wins" must mean HARD-configured wins — a request beaten by a default nobody
    // expressed an opinion about would make per-request inference unimplementable.
    [Fact]
    public async Task ShouldLetTheRequestSpeakWhereTheDeploymentSaidNothingAsync()
    {
        // given — a brain configured with no opinion on temperature or max tokens
        var broker = new CapturingGeneratorBroker();

        StandardAgent agent = AgentWith(broker)
            .Brain("http://localhost:9999/v1/", apiKey: "", model: "test");

        var request = new PromptRequest
        {
            Prompt = "hello",
            Temperature = 0.9,
            MaxTokens = 512
        };

        // when
        await agent.ProcessPromptAsync(request);

        // then — the request's values reached the wire
        broker.Captured.Should().NotBeNull();
        broker.Captured!.Temperature.Should().Be(0.9);
        broker.Captured.MaxTokens.Should().Be(512);
    }

    // Configuration is a ceiling (design §4): a temperature the deployment chose cannot be
    // moved by asking nicely.
    [Fact]
    public async Task ShouldHandTheBrokerTheConfiguredTemperatureOverTheRequestsAsync()
    {
        // given — a brain hard-configured on both knobs
        var broker = new CapturingGeneratorBroker();

        StandardAgent agent = AgentWith(broker)
            .Brain(
                "http://localhost:9999/v1/",
                apiKey: "",
                model: "test",
                temperature: 0.3,
                maxTokens: 256);

        var request = new PromptRequest
        {
            Prompt = "hello",
            Temperature = 0.9,
            MaxTokens = 4096
        };

        // when
        await agent.ProcessPromptAsync(request);

        // then — configured won, on both
        broker.Captured.Should().NotBeNull();
        broker.Captured!.Temperature.Should().Be(0.3);
        broker.Captured.MaxTokens.Should().Be(256);
    }

    // The third rung: nobody spoke, so the framework default applies — chosen by resolution at
    // the boundary, not by the broker (design §4.2).
    [Fact]
    public async Task ShouldHandTheBrokerTheFrameworkDefaultWhenNobodySaidAnythingAsync()
    {
        // given — no configured opinion, no request opinion
        var broker = new CapturingGeneratorBroker();
        StandardAgent agent = AgentWith(broker);

        // when
        await agent.ProcessPromptAsync(new PromptRequest { Prompt = "hello" });

        // then
        broker.Captured.Should().NotBeNull();
        broker.Captured!.Temperature.Should().Be(ResolvedInference.DefaultTemperature);
        broker.Captured.MaxTokens.Should().Be(ResolvedInference.DefaultMaxTokens);
    }

    // The streamed loop is a run like any other. A request whose temperature applied on the
    // batched call and silently vanished on the streamed one would make the stream the way to
    // step around the seam — and a control a caller can step around by changing method is not
    // a control (SPEC.md §7.6).
    [Fact]
    public async Task ShouldCarryTheRequestToTheBrokerOnTheStreamedPathAsync()
    {
        // given
        var broker = new CapturingGeneratorBroker();
        StandardAgent agent = AgentWith(broker);

        var request = new PromptRequest
        {
            Prompt = "hello",
            Temperature = 0.2,
            MaxTokens = 64
        };

        // when — the stream must be consumed for the run to happen
        await foreach (AgentStreamEvent _ in agent.StreamPromptAsync(request))
        {
        }

        // then
        broker.Captured.Should().NotBeNull();
        broker.Captured!.Temperature.Should().Be(0.2);
        broker.Captured.MaxTokens.Should().Be(64);
    }

    // Graceful degradation is a property (design §5): this broker never opts in, and the
    // streamed answer is STILL held to shape, because the guardian validates and revises on the
    // streamed path exactly as on the batched one.
    [Fact]
    public async Task ShouldHoldAStreamedAnswerToTheRequestSchemaAsync()
    {
        // given — a brain whose first streamed draft breaks the requested shape
        int call = 0;

        StandardAgent agent = AgentWith((systemPrompt, userPrompt) =>
            ValueTask.FromResult(call++ == 0
                ? "FINAL: Paris"
                : """FINAL: {"city":"Paris"}"""));

        var request = new PromptRequest
        {
            Prompt = "capital of France, as JSON",
            ResponseSchemaJson = """{"type":"object","required":["city"]}"""
        };

        // when
        List<AgentStreamEvent> events = [];

        await foreach (AgentStreamEvent streamEvent in agent.StreamPromptAsync(request))
        {
            events.Add(streamEvent);
        }

        // then — what streamed out as the answer is the revised draft, and only that
        List<string> responses =
            [.. events
                .Where(streamEvent => streamEvent.Type is AgentStreamEventType.Response)
                .Select(streamEvent => streamEvent.Content)];

        responses.Should().ContainSingle();
        responses[0].Should().Be("""{"city":"Paris"}""");
    }

    private sealed class CapturingNativeBroker : IGeneratorBrokerV1
    {
        public ResolvedInference? Captured { get; private set; }

        public bool HonorsRequest => true;

        public ValueTask<GenerationResult> GenerateAsync(
            IReadOnlyList<ConversationMessage> messages,
            IReadOnlyList<ToolDefinition> tools) =>
            ValueTask.FromResult(
                new GenerationResult { Content = "FINAL: ok" });

        public ValueTask<GenerationResult> GenerateAsync(
            IReadOnlyList<ConversationMessage> messages,
            IReadOnlyList<ToolDefinition> tools,
            ResolvedInference inference)
        {
            this.Captured = inference;

            return ValueTask.FromResult(
                new GenerationResult { Content = "FINAL: ok" });
        }
    }

    // The V1 path is where the context already reached the Brain foundation intact and was then
    // discarded (design §2). This pins the discarding stopped: the resolved options ride through
    // to the native broker exactly as they do on the text protocol.
    [Fact]
    public async Task ShouldCarryTheRequestToTheNativeBrokerAsync()
    {
        // given
        var broker = new CapturingNativeBroker();

        var skillBroker = new Mock<ISkillBroker>();

        skillBroker.Setup(b => b.SelectSkillsAsync())
            .ReturnsAsync(new List<Skill> { new() { Content = "You are a helpful agent." } });

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(b => b.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(b => b.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        StandardAgent agent = new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .UseNativeBrain(broker);

        var request = new PromptRequest
        {
            Prompt = "hello",
            Temperature = 0.2,
            MaxTokens = 64
        };

        // when
        await agent.ProcessPromptAsync(request);

        // then
        broker.Captured.Should().NotBeNull();
        broker.Captured!.Temperature.Should().Be(0.2);
        broker.Captured.MaxTokens.Should().Be(64);
    }
}
