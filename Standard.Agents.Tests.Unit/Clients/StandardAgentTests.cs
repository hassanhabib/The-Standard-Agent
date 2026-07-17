// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents;
using Standard.Agents.Brokers.Data;
using Standard.Agents.Brokers.Decision;
using Standard.Agents.Brokers.Direction;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Clients.Agents.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Clients;

public class StandardAgentTests
{
    // Every broker swapped, so nothing reaches a network. This is also exactly what
    // the conformance harness does (#39) — if the facade cannot be fully stubbed, the
    // vectors cannot run.
    private static StandardAgent CreateFullyStubbedAgent(string brainReply)
    {
        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(b => b.SelectSkillsAsync()).ReturnsAsync("you are an agent");

        var generatorBroker = new Mock<IGeneratorBroker>();
        generatorBroker.Setup(b => b.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(brainReply);

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(b => b.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();
        knowledgeBroker.Setup(b => b.SelectKnowledgeAsync(It.IsAny<string>())).ReturnsAsync([]);

        var classifierBroker = new Mock<IClassifierBroker>();
        classifierBroker.Setup(b => b.ClassifyAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("allow");

        var verifierBroker = new Mock<IVerifierBroker>();
        verifierBroker.Setup(b => b.VerifyAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(1.0);

        var mcpBroker = new Mock<IMcpBroker>();
        var logBroker = new Mock<ILogBroker>();

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseGenerator(generatorBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .UseGate(classifierBroker.Object)
            .UseJudge(verifierBroker.Object)
            .UseMcp(mcpBroker.Object)
            .UseLog(logBroker.Object);
    }

    [Fact]
    public async Task ShouldComposeAndProcessPromptAsync()
    {
        // given
        StandardAgent agent = CreateFullyStubbedAgent("FINAL: 42");

        // when
        string actualResult = await agent.ProcessPromptAsync("what is the answer?");

        // then
        actualResult.Should().Be("42");
    }

    // The builder is fluent — every method returns the same instance, so a chain
    // configures one agent rather than silently building several.
    [Fact]
    public void ShouldReturnSameInstanceOnEachBuilderMethod()
    {
        // given
        var agent = new StandardAgent();

        // when
        StandardAgent chained = agent
            .Skills("Skills")
            .Brain("https://x/", "key", "model")
            .Memory("memory.txt")
            .Knowledge("Knowledge")
            .Mcp("https://mcp/")
            .LogTo("log.txt");

        // then
        chained.Should().BeSameAs(agent);
    }

    // ⭐ Configuration set AFTER a prompt must take effect. The composition is cached,
    // so a builder method that did not drop it would return `this` and silently ignore
    // the change — the caller would see the old agent and no error explaining why.
    [Fact]
    public async Task ShouldRecomposeAfterConfigurationChangesAsync()
    {
        // given
        StandardAgent agent = CreateFullyStubbedAgent("FINAL: first");

        string firstResult = await agent.ProcessPromptAsync("prompt");

        var newGenerator = new Mock<IGeneratorBroker>();
        newGenerator.Setup(b => b.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("FINAL: second");

        // when
        agent.UseGenerator(newGenerator.Object);
        string secondResult = await agent.ProcessPromptAsync("prompt");

        // then
        firstResult.Should().Be("first");
        secondResult.Should().Be("second");
    }

    // An agent with no brain is not an agent. Theory Ch.5: "An agent has one brain."
    // Zero is not a valid count, and failing at composition names the mistake where it
    // was made rather than as a null-reference on the first prompt.
    [Fact]
    public async Task ShouldThrowCompositionExceptionOnProcessPromptIfBrainIsNotConfiguredAsync()
    {
        // given
        var agent = new StandardAgent();

        // when
        ValueTask<string> processTask = agent.ProcessPromptAsync("prompt");

        InvalidAgentCompositionException actualException =
            await Assert.ThrowsAsync<InvalidAgentCompositionException>(
                processTask.AsTask);

        // then
        actualException.Message.Should().Contain("no brain");
    }

    // ⚠️ A swapped generator with no Brain() settings must still compose.
    //
    // Gate and Judge fall back to the Brain's endpoint settings, so a caller who
    // supplies a generator broker but never calls Brain() leaves those settings null.
    // Building the default Classifier/Verifier from them would dereference null — and
    // the conformance harness (#39) is exactly this caller.
    [Fact]
    public async Task ShouldComposeOnProcessPromptIfGeneratorIsSwappedWithoutBrainSettingsAsync()
    {
        // given
        var generatorBroker = new Mock<IGeneratorBroker>();
        generatorBroker.Setup(b => b.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("FINAL: ok");

        var classifierBroker = new Mock<IClassifierBroker>();
        classifierBroker.Setup(b => b.ClassifyAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("allow");

        var verifierBroker = new Mock<IVerifierBroker>();
        verifierBroker.Setup(b => b.VerifyAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(1.0);

        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(b => b.SelectSkillsAsync()).ReturnsAsync("skills");

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(b => b.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();
        var logBroker = new Mock<ILogBroker>();

        // No Brain(...) call anywhere.
        var agent = new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseGenerator(generatorBroker.Object)
            .UseGate(classifierBroker.Object)
            .UseJudge(verifierBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .UseMcp(new Mock<IMcpBroker>().Object)
            .UseLog(logBroker.Object);

        // when
        string actualResult = await agent.ProcessPromptAsync("prompt");

        // then
        actualResult.Should().Be("ok");
    }

    // A swapped generator with an UNSWAPPED gate has nowhere to get the gate's
    // endpoint from. That must be a named composition error, not a null-reference from
    // inside a broker constructor.
    [Fact]
    public async Task ShouldThrowCompositionExceptionOnProcessPromptIfGateHasNoSettingsAsync()
    {
        // given
        var generatorBroker = new Mock<IGeneratorBroker>();

        var agent = new StandardAgent()
            .UseGenerator(generatorBroker.Object);

        // when
        ValueTask<string> processTask = agent.ProcessPromptAsync("prompt");

        InvalidAgentCompositionException actualException =
            await Assert.ThrowsAsync<InvalidAgentCompositionException>(
                processTask.AsTask);

        // then
        actualException.Message.Should().Contain("gate");
    }
}
