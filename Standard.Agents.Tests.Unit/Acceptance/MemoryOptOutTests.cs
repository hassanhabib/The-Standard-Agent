// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Models.Foundations.Skills;
using Xunit;

namespace Standard.Agents.Tests.Unit.Acceptance;

// Found in the 2026-09-04 principal review (F-05): every instance composed memory by default and
// always registered the remember tool, so a host serving many callers from one singleton let
// one caller's remembered facts reach another caller's model, and let one caller poison memory
// for everyone after. Memory stays the convenient default for a one-user agent; a deployment
// that serves strangers says so, and the agent then recalls nothing and offers no way to store.
public class MemoryOptOutTests
{
    private const string RememberedFact = "the user is Ada";

    private static IMemoryBroker MemoryHolding(string fact, out Mock<IMemoryBroker> memoryBroker)
    {
        memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([fact]);

        return memoryBroker.Object;
    }

    private static IKnowledgeBroker EmptyKnowledge()
    {
        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        return knowledgeBroker.Object;
    }

    [Fact]
    public async Task ShouldNotAdvertiseOrRecallMemoryWhenComposedWithoutMemoryAsync()
    {
        // given
        IMemoryBroker memory = MemoryHolding(RememberedFact, out Mock<IMemoryBroker> memoryBroker);
        string seenByBrain = string.Empty;

        StandardAgent agent = new StandardAgent()
            .UseKnowledge(EmptyKnowledge())
            .OnSkills(async () =>
                [new Skill { Name = "persona", Content = "Your tools:\n{{tools}}" }])
            .UseMemory(memory)
            .WithoutMemory()
            .OnBrain(async (systemPrompt, userPrompt) =>
            {
                seenByBrain = systemPrompt + "\n" + userPrompt;

                return "FINAL: done";
            });

        // when
        await agent.ProcessPromptAsync("hello");

        // then: nothing recalled, nothing offered, the store never asked
        seenByBrain.Should().NotContain(RememberedFact);
        seenByBrain.Should().NotContain("remember");
        memoryBroker.Verify(broker => broker.SelectMemoriesAsync(), Times.Never);
    }

    [Fact]
    public async Task ShouldAdvertiseAndRecallMemoryByDefaultAsync()
    {
        // given: the unamended one-user default
        IMemoryBroker memory = MemoryHolding(RememberedFact, out Mock<IMemoryBroker> memoryBroker);
        string seenByBrain = string.Empty;

        StandardAgent agent = new StandardAgent()
            .UseKnowledge(EmptyKnowledge())
            .OnSkills(async () =>
                [new Skill { Name = "persona", Content = "Your tools:\n{{tools}}" }])
            .UseMemory(memory)
            .OnBrain(async (systemPrompt, userPrompt) =>
            {
                seenByBrain = systemPrompt + "\n" + userPrompt;

                return "FINAL: done";
            });

        // when
        await agent.ProcessPromptAsync("hello");

        // then
        seenByBrain.Should().Contain(RememberedFact);
        seenByBrain.Should().Contain("remember");
        memoryBroker.Verify(broker => broker.SelectMemoriesAsync(), Times.Once);
    }

    // The document says it the same way code does: a document is the whole truth of a hosted
    // agent, and a hosted agent that wants no memory needs one key to say so.
    [Fact]
    public async Task ShouldComposeWithoutMemoryFromJsonWhenTheDocumentSaysFalseAsync()
    {
        // given
        IMemoryBroker memory = MemoryHolding(RememberedFact, out Mock<IMemoryBroker> memoryBroker);
        string seenByBrain = string.Empty;

        StandardAgent agent = StandardAgent.FromJson("""{ "memory": false }""")
            .UseKnowledge(EmptyKnowledge())
            .OnSkills(async () =>
                [new Skill { Name = "persona", Content = "Your tools:\n{{tools}}" }])
            .UseMemory(memory)
            .OnBrain(async (systemPrompt, userPrompt) =>
            {
                seenByBrain = systemPrompt + "\n" + userPrompt;

                return "FINAL: done";
            });

        // when
        await agent.ProcessPromptAsync("hello");

        // then
        seenByBrain.Should().NotContain(RememberedFact);
        seenByBrain.Should().NotContain("remember");
        memoryBroker.Verify(broker => broker.SelectMemoriesAsync(), Times.Never);
    }
}
