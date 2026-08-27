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

// Say so in the trace (docs/per-request-inference.md §4.3). The framework already narrates its
// guardian decisions on the stated principle that a rejection the trace does not explain is a
// turn nobody can account for. A caller whose schema was discarded, whose passthrough key was
// stripped, or whose options quietly degraded to guardian-only deserves the same courtesy.
public class TraceRequestTests : IDisposable
{
    private readonly string logPath = Path.GetTempFileName();

    public void Dispose()
    {
        if (File.Exists(this.logPath))
        {
            File.Delete(this.logPath);
        }
    }

    private StandardAgent AgentWith(Func<string, string, ValueTask<string>> brain)
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
            .LogTo(this.logPath)
            .OnBrain(brain);
    }

    [Fact]
    public async Task ShouldAnnounceADiscardedRequestSchemaAsync()
    {
        // given — a configured Contract, and a caller who sent a schema anyway
        StandardAgent agent = AgentWith((systemPrompt, userPrompt) =>
            ValueTask.FromResult("""FINAL: {"city":"Paris"}"""))
            .Contract("""{"type":"object","required":["city"]}""");

        var request = new PromptRequest
        {
            Prompt = "capital of France, as JSON",
            ResponseSchemaJson = """{"type":"object","required":["name"]}"""
        };

        // when
        await agent.ProcessPromptAsync(request);

        // then — the caller got a differently-shaped answer, and the trace says why
        string trace = await File.ReadAllTextAsync(this.logPath);
        trace.Should().Contain("request schema discarded");
    }

    [Fact]
    public async Task ShouldAnnounceAStrippedCoreOwnedKeyAsync()
    {
        // given — a passthrough trying to raise max_tokens past what precedence resolved
        StandardAgent agent = AgentWith((systemPrompt, userPrompt) =>
            ValueTask.FromResult("FINAL: ok"));

        var request = new PromptRequest
        {
            Prompt = "hello",
            ProviderOptionsJson = """{"max_tokens": 999999, "grammar": "root ::= object"}"""
        };

        // when
        await agent.ProcessPromptAsync(request);

        // then
        string trace = await File.ReadAllTextAsync(this.logPath);
        trace.Should().Contain("max_tokens");
        trace.Should().Contain("stripped");
    }

    [Fact]
    public async Task ShouldAnnounceABrokerThatDoesNotHonorRequestsAsync()
    {
        // given — a Custom brain that never opts in, and a caller with opinions
        StandardAgent agent = AgentWith((systemPrompt, userPrompt) =>
            ValueTask.FromResult("FINAL: ok"));

        var request = new PromptRequest
        {
            Prompt = "hello",
            Temperature = 0.2
        };

        // when
        await agent.ProcessPromptAsync(request);

        // then — degradation is graceful AND announced: shape is enforced by the guardian only,
        // and the trace can say which of the two happened
        string trace = await File.ReadAllTextAsync(this.logPath);
        trace.Should().Contain("does not honor");
    }
}
