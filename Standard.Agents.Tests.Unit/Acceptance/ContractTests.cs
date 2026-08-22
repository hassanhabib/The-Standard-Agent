// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Models.Foundations.Skills;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// The agent answers with a string, and that is the entire client contract. Every consumer wiring
// an agent into a workflow therefore writes a parser — which is exactly what the framework talked
// them out of doing everywhere else. Native tool calling gives structured input TO tools; the
// agent's own answer has always been prose.
//
// A schema check asks whether an answer is the right SHAPE, immediately beside the Judge asking
// whether it is good ENOUGH. Same moment, same kind of verdict, so it is a third guardian rather
// than a new concept — and a mismatch is a re-think, not a fault: it takes the Judge's revision
// path with the validation error as the reason the draft was rejected.
public class ContractTests
{
    private const string InvoiceSchema =
        """{ "type": "object", "required": ["amount", "currency"] }""";

    private static StandardAgent AgentAnswering(params string[] replies)
    {
        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        int asked = 0;

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .OnBrain((_, _) =>
            {
                string reply = replies[Math.Min(asked, replies.Length - 1)];
                asked++;

                return ValueTask.FromResult(reply);
            });
    }

    [Fact]
    public async Task ShouldReturnTheAnswerWhenItSatisfiesTheContractAsync()
    {
        // given
        StandardAgent agent = AgentAnswering(
            """FINAL: { "amount": 100, "currency": "USD" }""")
                .Contract(InvoiceSchema);

        // when
        string actualAnswer = await agent.ProcessPromptAsync("what is owed");

        // then
        actualAnswer.Should().Contain("\"amount\"").And.Contain("\"currency\"");
    }

    // The whole point: a shape violation is a revision signal, not an exception and not a silent
    // pass. The model is told what was wrong and asked again within the turn budget.
    [Fact]
    public async Task ShouldReviseWhenTheAnswerDoesNotSatisfyTheContractAsync()
    {
        // given — prose first, then the shape that was asked for
        StandardAgent agent = AgentAnswering(
            "FINAL: The amount is one hundred US dollars.",
            """FINAL: { "amount": 100, "currency": "USD" }""")
                .Contract(InvoiceSchema)
                .MaxTurns(5);

        // when
        string actualAnswer = await agent.ProcessPromptAsync("what is owed");

        // then
        actualAnswer.Should().Contain(
            "\"amount\"",
            because: "a draft that does not satisfy the contract is re-thought, and the second "
                + "draft is the one the caller receives");

        actualAnswer.Should().NotContain("one hundred US dollars");
    }

    // A model that never produces the shape must fail the way every other exhausted control fails:
    // a graceful refusal, distinguishable from an answer. Never an exception, and never prose
    // handed back as though it had satisfied a schema it did not.
    [Fact]
    public async Task ShouldRefuseWhenTheContractIsNeverSatisfiedAsync()
    {
        // given
        StandardAgent agent = AgentAnswering("FINAL: The amount is one hundred US dollars.")
            .Contract(InvoiceSchema)
            .MaxTurns(3);

        // when
        string actualAnswer = await agent.ProcessPromptAsync("what is owed");

        // then
        actualAnswer.Should().NotBe(
            "The amount is one hundred US dollars.",
            because: "prose handed back unchanged is the failure this exists to prevent — a "
                + "caller parsing the result would have no way to tell it never matched");

        actualAnswer.Should().NotBeNullOrWhiteSpace(
            because: "an exhausted contract refuses gracefully, the same as an exhausted "
                + "revision budget, rather than throwing or returning nothing");
    }

    // Wide open by default: an agent given no contract is not constrained by having become
    // checkable. Same rule as counting versus blocking in the budget.
    [Fact]
    public async Task ShouldNotConstrainAnAgentGivenNoContractAsync()
    {
        // given
        StandardAgent agent = AgentAnswering("FINAL: The amount is one hundred US dollars.");

        // when
        string actualAnswer = await agent.ProcessPromptAsync("what is owed");

        // then
        actualAnswer.Should().Be("The amount is one hundred US dollars.");
    }
}
