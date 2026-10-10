// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Foundations.Knowledges;
using Standard.Agents.Models.Foundations.Skills;
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// SPEC.md §4.2: a grounded answer cites its sources.
//
// The agent cites, not the model. Asking a model to name its sources produces a citation when the
// model feels like writing one, which is not a citation anyone can rely on — a bank's policy bot,
// a helpdesk answering from runbooks and a coding agent answering from API references all need
// the line to be a fact the run knows. These pin the rules that make it one: opt-in, after the
// Judge, the same on every door, and never on a run that did not answer.
public class KnowledgeCitationTests : IDisposable
{
    private const string RefundsSource = "Refund Policy (2026)";
    private const string ShippingSource = "Shipping Handbook";
    private const string Answer = "Enterprise customers may request a refund within 90 days.";

    private readonly string workPath =
        Path.Combine(Path.GetTempPath(), $"citation-{Guid.NewGuid():n}");

    private static IReadOnlyList<KnowledgeResult> GroundingResults() =>
    [
        new KnowledgeResult
        {
            Text = "Refund policy. Enterprise customers may request a refund within 90 days.",
            Score = 0.91,
            Source = RefundsSource
        },
        new KnowledgeResult
        {
            Text = "Refunds go back to the original payment method.",
            Score = 0.42,
            Source = ShippingSource
        }
    ];

    private static StandardAgent GroundedAgent(Func<string, string, ValueTask<string>> brain)
    {
        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        IReadOnlyList<KnowledgeResult> knowledgeResults = GroundingResults();

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .OnSourcedKnowledge(async query => knowledgeResults)
            .OnBrain(brain);
    }

    private static Func<string, string, ValueTask<string>> Answering(string answer) =>
        async (systemPrompt, userPrompt) => $"FINAL: {answer}";

    [Fact]
    public async Task ShouldCiteTheSourcesThatGroundedAnAnswerAsync()
    {
        // given
        StandardAgent agent = GroundedAgent(Answering(Answer)).CiteKnowledge();

        string expectedResult =
            $"{Answer}\n\nSource: {RefundsSource}\nSource: {ShippingSource}";

        // when
        string actualResult = await agent.ProcessPromptAsync("what is the refund window?");

        // then
        actualResult.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ShouldReturnTheAnswerUnchangedWhenCitationWasNeverAskedForAsync()
    {
        // given
        StandardAgent agent = GroundedAgent(Answering(Answer));

        // when
        string actualResult = await agent.ProcessPromptAsync("what is the refund window?");

        // then — byte for byte the answer an agent without citation always returned
        actualResult.Should().Be(Answer);
    }

    // The passage reaches the brain exactly as an unsourced broker's would (SPEC.md §3.7): the
    // source rides the run, not the observation, so the model is shown what it was always shown.
    [Fact]
    public async Task ShouldShowTheBrainThePassageWithoutItsSourceAsync()
    {
        // given
        string seenByBrain = string.Empty;

        StandardAgent agent = GroundedAgent(async (systemPrompt, userPrompt) =>
        {
            seenByBrain = userPrompt;

            return $"FINAL: {Answer}";
        })
            .CiteKnowledge();

        // when
        await agent.ProcessPromptAsync("what is the refund window?");

        // then
        seenByBrain.Should().Contain("within 90 days");
        seenByBrain.Should().NotContain(RefundsSource);
    }

    [Fact]
    public async Task ShouldNotRepeatASourceTheAnswerAlreadyCreditsAsync()
    {
        // given
        string creditedAnswer = $"{Answer}\n\nsource: {RefundsSource}";
        StandardAgent agent = GroundedAgent(Answering(creditedAnswer)).CiteKnowledge();
        string expectedResult = $"{creditedAnswer}\n\nSource: {ShippingSource}";

        // when
        string actualResult = await agent.ProcessPromptAsync("what is the refund window?");

        // then
        actualResult.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ShouldCiteWithTheConfiguredPrefixAsync()
    {
        // given
        StandardAgent agent =
            GroundedAgent(Answering(Answer)).CiteKnowledge(prefix: "Reference: ");

        string expectedResult =
            $"{Answer}\n\nReference: {RefundsSource}\nReference: {ShippingSource}";

        // when
        string actualResult = await agent.ProcessPromptAsync("what is the refund window?");

        // then
        actualResult.Should().Be(expectedResult);
    }

    // A passage whose origin is unknown is never cited (SPEC.md §3.7) — which is every passage a
    // plain broker returns, so an agent that turns citation on over a plain broker answers as it
    // always did rather than crediting a blank.
    [Fact]
    public async Task ShouldNotCiteKnowledgeWhoseSourceIsUnknownAsync()
    {
        // given
        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        IReadOnlyList<string> passages =
            ["Refund policy. Enterprise customers may request a refund within 90 days."];

        StandardAgent agent = new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .OnKnowledge(async query => passages)
            .OnBrain(Answering(Answer))
            .CiteKnowledge();

        // when
        string actualResult = await agent.ProcessPromptAsync("what is the refund window?");

        // then
        actualResult.Should().Be(Answer);
    }

    [Fact]
    public async Task ShouldNotCiteARunThatDidNotAnswerAsync()
    {
        // given — the knowledge is recalled, then the Gate refuses the task
        StandardAgent agent = GroundedAgent(Answering(Answer))
            .OnGate(async (gatePrompt, input) => "refuse: out of scope")
            .CiteKnowledge();

        // when
        AgentOutcome actualOutcome = await agent.RunAsync("what is the refund window?");

        // then
        actualOutcome.Status.Should().Be(AgentStatus.Refused);
        actualOutcome.Result.Should().NotContain("Source:");
    }

    // An answer held to a response schema is a document the caller will parse; a line appended
    // after it breaks the very shape the caller was promised (SPEC.md §4.2).
    [Fact]
    public async Task ShouldNotCiteAnAnswerHeldToAResponseSchemaAsync()
    {
        // given
        string jsonAnswer = """{"refundDays":90}""";
        StandardAgent agent = GroundedAgent(Answering(jsonAnswer)).CiteKnowledge();

        var request = new PromptRequest
        {
            Prompt = "what is the refund window?",
            ResponseSchemaJson = """{"type":"object"}"""
        };

        // when
        string actualResult = await agent.ProcessPromptAsync(request);

        // then
        actualResult.Should().Be(jsonAnswer);
    }

    // The Judge scores the model's answer; lines the agent wrote are not the model's work, and a
    // Judge shown them is scoring the agent (SPEC.md §4.2).
    [Fact]
    public async Task ShouldCiteAfterTheJudgeAsync()
    {
        // given
        List<string> seenByJudge = [];

        StandardAgent agent = GroundedAgent(Answering(Answer))
            .OnJudge(async (judgePrompt, candidate) =>
            {
                seenByJudge.Add(candidate);

                return "1.0";
            })
            .CiteKnowledge();

        // when
        string actualResult = await agent.ProcessPromptAsync("what is the refund window?");

        // then
        seenByJudge.Should().NotBeEmpty();
        seenByJudge.Should().AllSatisfy(candidate => candidate.Should().NotContain("Source:"));
        actualResult.Should().EndWith($"Source: {ShippingSource}");
    }

    [Fact]
    public async Task ShouldCiteWhenTheRequestAsksAndTheAgentExpressedNoOpinionAsync()
    {
        // given
        StandardAgent agent = GroundedAgent(Answering(Answer));

        var request = new PromptRequest
        {
            Prompt = "what is the refund window?",
            CiteKnowledge = true
        };

        // when
        string actualResult = await agent.ProcessPromptAsync(request);

        // then
        actualResult.Should().EndWith($"Source: {RefundsSource}\nSource: {ShippingSource}");
    }

    // A deployment that must cite — a regulated answer — cannot be switched off by a caller.
    [Fact]
    public async Task ShouldKeepCitingWhenTheDeploymentRequiresItAndTheRequestDeclinesAsync()
    {
        // given
        StandardAgent agent = GroundedAgent(Answering(Answer)).CiteKnowledge();

        var request = new PromptRequest
        {
            Prompt = "what is the refund window?",
            CiteKnowledge = false
        };

        // when
        string actualResult = await agent.ProcessPromptAsync(request);

        // then
        actualResult.Should().EndWith($"Source: {RefundsSource}\nSource: {ShippingSource}");
    }

    // A deployment that never cites cannot be switched on by a caller either.
    [Fact]
    public async Task ShouldNotCiteWhenTheDeploymentForbidsItAndTheRequestAsksAsync()
    {
        // given
        StandardAgent agent = GroundedAgent(Answering(Answer)).CiteKnowledge(cite: false);

        var request = new PromptRequest
        {
            Prompt = "what is the refund window?",
            CiteKnowledge = true
        };

        // when
        string actualResult = await agent.ProcessPromptAsync(request);

        // then
        actualResult.Should().Be(Answer);
    }

    // One answer on every door (SPEC.md §4.2, §4.14): the streamed response and the streamed
    // outcome carry the same cited text the batched door returns.
    [Fact]
    public async Task ShouldCiteTheSameAnswerOnTheStreamedDoorAsync()
    {
        // given
        StandardAgent agent = GroundedAgent(Answering(Answer)).CiteKnowledge();
        string expectedResult = await agent.ProcessPromptAsync("what is the refund window?");
        var request = new PromptRequest { Prompt = "what is the refund window?" };

        // when
        AgentRunStream runStream = agent.RunStreamAsync(request);
        List<AgentStreamEvent> streamedEvents = [];

        await foreach (AgentStreamEvent streamedEvent in runStream)
        {
            streamedEvents.Add(streamedEvent);
        }

        // then
        expectedResult.Should().EndWith($"Source: {ShippingSource}");

        streamedEvents.Should().Contain(streamedEvent =>
            streamedEvent.Type == AgentStreamEventType.Response
                && streamedEvent.Content == expectedResult);

        runStream.Outcome.Result.Should().Be(expectedResult);
    }

    // The session records the answer the caller was given (SPEC.md §4.11), so the next prompt in
    // the conversation is told what the agent actually said.
    [Fact]
    public async Task ShouldRecordTheCitedAnswerInTheSessionAsync()
    {
        // given
        string seenByBrain = string.Empty;

        StandardAgent agent = GroundedAgent(async (systemPrompt, userPrompt) =>
        {
            seenByBrain = userPrompt;

            return $"FINAL: {Answer}";
        })
            .Sessions(this.workPath)
            .CiteKnowledge();

        // when
        await agent.ProcessPromptAsync("what is the refund window?", sessionId: "customer-1");
        await agent.ProcessPromptAsync("and for small businesses?", sessionId: "customer-1");

        // then
        seenByBrain.Should().Contain($"Source: {RefundsSource}");
    }

    // The built-in knowledge folder is citable out of the box (SPEC.md §4.2): the source is the
    // document's path relative to the folder.
    [Fact]
    public async Task ShouldCiteTheBuiltInKnowledgeFolderByPathAsync()
    {
        // given
        string knowledgePath = Path.Combine(this.workPath, "knowledge");
        Directory.CreateDirectory(Path.Combine(knowledgePath, "policies"));

        await File.WriteAllTextAsync(
            Path.Combine(knowledgePath, "policies", "refunds.md"),
            "Refund policy. Enterprise customers may request a refund within 90 days.");

        await File.WriteAllTextAsync(
            Path.Combine(knowledgePath, "holidays.md"),
            "The office is closed on public holidays.");

        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        StandardAgent agent = new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .Knowledge(knowledgePath, maxResults: 1)
            .OnBrain(Answering(Answer))
            .CiteKnowledge();

        // when
        string actualResult =
            await agent.ProcessPromptAsync("what is our refund policy for enterprise customers");

        // then
        actualResult.Should().Be($"{Answer}\n\nSource: policies/refunds.md");
    }

    public void Dispose()
    {
        if (Directory.Exists(this.workPath))
        {
            Directory.Delete(this.workPath, recursive: true);
        }
    }
}
