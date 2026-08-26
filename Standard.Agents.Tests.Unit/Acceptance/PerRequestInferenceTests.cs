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
}
