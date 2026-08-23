// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Files;
using Standard.Agents.Services.Foundations.Knowledges;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// SPEC.md §4.2: retrieval answers "what is relevant to this task", not "what exists".
//
// The regression this pins is the one that made the feature look configured and return nothing:
// matching the whole prompt literally against whole documents. A natural question never appears
// verbatim inside its own answer, so the old implementation retrieved zero results for exactly
// the queries a user asks.
public class RetrievalTests : IDisposable
{
    private readonly string knowledgePath =
        Path.Combine(Path.GetTempPath(), $"knowledge-{Guid.NewGuid():n}");

    private IKnowledgeService KnowledgeOver(params (string Name, string Body)[] documents)
    {
        Directory.CreateDirectory(this.knowledgePath);

        foreach ((string name, string body) in documents)
        {
            File.WriteAllText(Path.Combine(this.knowledgePath, name), body);
        }

        return new KnowledgeService(
            fileBroker: new FileBroker(),
            knowledgePath: this.knowledgePath,
            searchPattern: "*.md",
            maxResults: 1,
            loggingBroker: new Mock<ILoggingBroker>().Object);
    }

    // Found in the 2026-08-23 docs pass, sweep shape 3 — a parameter taken and never used:
    // .Knowledge(minScore:) stored the floor in the builder and composition never passed it on,
    // so a host raising the relevance floor changed nothing and was never told. The foundation
    // honoured a floor all along; only the seam dropped it.
    [Fact]
    public async Task ShouldHoldKnowledgeToTheConfiguredRelevanceFloorAsync()
    {
        // given — one weakly-relevant document, and a floor set far above any real score
        Directory.CreateDirectory(this.knowledgePath);

        File.WriteAllText(
            Path.Combine(this.knowledgePath, "pricing.md"),
            "The pro plan pricing is $29 per month, billed annually.");

        string seenByBrain = string.Empty;

        StandardAgent agent = new StandardAgent()
            .OnBrain((_, userPrompt) =>
            {
                seenByBrain = userPrompt;

                return ValueTask.FromResult("FINAL: ok");
            })
            .OnSkills(() => ValueTask.FromResult<IReadOnlyList<Models.Foundations.Skills.Skill>>([]))
            .OnMemory(
                recall: () => ValueTask.FromResult<IReadOnlyList<string>>([]),
                remember: _ => ValueTask.CompletedTask)
            .Knowledge(this.knowledgePath, minScore: 1_000.0);

        // when
        await agent.ProcessPromptAsync("pro plan pricing");

        // then — nothing clears a floor of a thousand; injecting anyway means the floor is dead
        seenByBrain.Should().NotContain(
            "$29",
            because: "the host raised the relevance floor and the composition must carry it "
                + "to the foundation that enforces it");
    }

    [Fact]
    public async Task ShouldRetrieveThePassageThatAnswersANaturalQuestionAsync()
    {
        // given
        IKnowledgeService knowledgeService = KnowledgeOver(
            ("refunds.md",
                "Refund policy. Enterprise customers may request a refund within 90 days of "
                    + "purchase. Refunds are issued to the original payment method."),
            ("shipping.md",
                "Shipping policy. Orders ship within two business days. Expedited shipping is "
                    + "available for an additional fee."),
            ("holidays.md",
                "The office is closed on public holidays and reopens the next business day."));

        // when — a question, phrased as a person would phrase it
        IReadOnlyList<string> actualPassages = await knowledgeService.RetrieveKnowledgeAsync(
            query: "what is our refund policy for enterprise customers?");

        // then
        actualPassages.Should().ContainSingle();
        actualPassages[0].Should().Contain("90 days");
    }

    [Fact]
    public async Task ShouldPreferTheRarerTermWhenRankingAsync()
    {
        // given — "policy" is in every document, so it says nothing; "refund" singles one out
        IKnowledgeService knowledgeService = KnowledgeOver(
            ("a.md", "Shipping policy. This policy covers delivery."),
            ("b.md", "Returns policy. This policy covers refund requests and their handling."),
            ("c.md", "Privacy policy. This policy covers data."));

        // when
        IReadOnlyList<string> actualPassages =
            await knowledgeService.RetrieveKnowledgeAsync(query: "refund policy");

        // then
        actualPassages.Should().ContainSingle();
        actualPassages[0].Should().Contain("refund");
    }

    [Fact]
    public async Task ShouldRetrieveNothingWhenNothingIsRelevantAsync()
    {
        // given
        IKnowledgeService knowledgeService = KnowledgeOver(
            ("a.md", "Shipping policy. Orders ship within two business days."));

        // when
        IReadOnlyList<string> actualPassages =
            await knowledgeService.RetrieveKnowledgeAsync(query: "quantum chromodynamics");

        // then — silence is the right answer when nothing matches
        actualPassages.Should().BeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(this.knowledgePath))
        {
            Directory.Delete(this.knowledgePath, recursive: true);
        }
    }
}
