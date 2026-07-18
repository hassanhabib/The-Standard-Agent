// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Classifiers;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Logs;
using Standard.Agents.Brokers.Mcps;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Brokers.Verifiers;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Clients.Agents.Exceptions;
using Xunit;

namespace Standard.Agents.Tests.Unit.Clients;

public class StandardAgentTests
{
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
        classifierBroker.Setup(b => b.ClassifyAsync(It.IsAny<string>()))
            .ReturnsAsync("allow");

        var verifierBroker = new Mock<IVerifierBroker>();
        verifierBroker.Setup(b => b.VerifyAsync(It.IsAny<string>()))
            .ReturnsAsync("1.0");

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
        string actualResult = await agent.ProcessPromptAsync(prompt: "what is the answer?");

        // then
        actualResult.Should().Be("42");
    }

    [Fact]
    public void ShouldReturnSameInstanceOnEachBuilderMethod()
    {
        // given
        var agent = new StandardAgent();

        // when
        StandardAgent chained = agent
            .Skills(path: "Skills")
            .Brain(apiUrl: "https://x/", apiKey: "key", model: "model")
            .Memory(path: "memory.txt")
            .Knowledge(path: "Knowledge")
            .Mcp(endpointUrl: "https://mcp/")
            .LogTo(path: "log.txt");

        // then
        chained.Should().BeSameAs(agent);
    }

    [Fact]
    public async Task ShouldRecomposeAfterConfigurationChangesAsync()
    {
        // given
        StandardAgent agent = CreateFullyStubbedAgent("FINAL: first");

        string firstResult = await agent.ProcessPromptAsync(prompt: "prompt");

        var newGenerator = new Mock<IGeneratorBroker>();
        newGenerator.Setup(b => b.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("FINAL: second");

        // when
        agent.UseGenerator(newGenerator.Object);
        string secondResult = await agent.ProcessPromptAsync(prompt: "prompt");

        // then
        firstResult.Should().Be("first");
        secondResult.Should().Be("second");
    }

    [Fact]
    public async Task ShouldThrowCompositionExceptionOnProcessPromptIfBrainIsNotConfiguredAsync()
    {
        // given
        var agent = new StandardAgent();

        // when
        ValueTask<string> processTask = agent.ProcessPromptAsync(prompt: "prompt");

        InvalidAgentCompositionException actualException =
            await Assert.ThrowsAsync<InvalidAgentCompositionException>(
                processTask.AsTask);

        // then
        actualException.Message.Should().Contain("no brain");
    }

    [Fact]
    public async Task ShouldComposeOnProcessPromptIfGeneratorIsSwappedWithoutBrainSettingsAsync()
    {
        // given
        var generatorBroker = new Mock<IGeneratorBroker>();
        generatorBroker.Setup(b => b.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("FINAL: ok");

        var classifierBroker = new Mock<IClassifierBroker>();
        classifierBroker.Setup(b => b.ClassifyAsync(It.IsAny<string>()))
            .ReturnsAsync("allow");

        var verifierBroker = new Mock<IVerifierBroker>();
        verifierBroker.Setup(b => b.VerifyAsync(It.IsAny<string>()))
            .ReturnsAsync("1.0");

        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(b => b.SelectSkillsAsync()).ReturnsAsync("skills");

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(b => b.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();
        var logBroker = new Mock<ILogBroker>();

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
        string actualResult = await agent.ProcessPromptAsync(prompt: "prompt");

        // then
        actualResult.Should().Be("ok");
    }

    [Fact]
    public async Task ShouldComposeBareBrainOnProcessPromptWithoutGuardiansAsync()
    {
        // given
        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(b => b.SelectSkillsAsync()).ReturnsAsync(string.Empty);

        var memory = new Mock<IMemoryBroker>();
        memory.Setup(b => b.SelectMemoriesAsync()).ReturnsAsync([]);

        var generatorBroker = new Mock<IGeneratorBroker>();
        generatorBroker.Setup(b => b.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("Hello there!");

        var agent = new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memory.Object)
            .UseGenerator(generatorBroker.Object)
            .UseLog(new Mock<ILogBroker>().Object);

        // when
        string actualResult = await agent.ProcessPromptAsync(prompt: "Hi there!");

        // then
        actualResult.Should().Be("Hello there!");
    }

    [Fact]
    public async Task ShouldComposeCoreProfileAgentOnProcessPromptWithoutMcpConfiguredAsync()
    {
        // given
        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(b => b.SelectSkillsAsync()).ReturnsAsync("skills");

        var generatorBroker = new Mock<IGeneratorBroker>();
        generatorBroker.Setup(b => b.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("FINAL: composed");

        var gate = new Mock<IClassifierBroker>();
        gate.Setup(b => b.ClassifyAsync(It.IsAny<string>()))
            .ReturnsAsync("allow");

        var judge = new Mock<IVerifierBroker>();
        judge.Setup(b => b.VerifyAsync(It.IsAny<string>()))
            .ReturnsAsync("1.0");

        var memory = new Mock<IMemoryBroker>();
        memory.Setup(b => b.SelectMemoriesAsync()).ReturnsAsync([]);

        var agent = new StandardAgent()
    .UseSkills(skillBroker.Object)
    .UseGenerator(generatorBroker.Object)
    .UseGate(gate.Object)
    .UseJudge(judge.Object)
    .UseMemory(memory.Object)
    .UseKnowledge(new Mock<IKnowledgeBroker>().Object)
    .UseLog(new Mock<ILogBroker>().Object);

        // when
        string actualResult = await agent.ProcessPromptAsync(prompt: "prompt");

        // then
        actualResult.Should().Be("composed");
    }

    [Fact]
    public async Task ShouldRecoverFromUnknownToolWithoutMcpConfiguredAsync()
    {
        // given — the Brain calls a tool that does not exist, then answers
        var replies = new Queue<string>(
        [
            "ACTION: weather: today",
            "FINAL: could not get weather"
        ]);

        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(b => b.SelectSkillsAsync()).ReturnsAsync("skills");

        var generatorBroker = new Mock<IGeneratorBroker>();
        generatorBroker.Setup(b => b.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(() => replies.Dequeue());

        var gate = new Mock<IClassifierBroker>();
        gate.Setup(b => b.ClassifyAsync(It.IsAny<string>()))
            .ReturnsAsync("allow");

        var judge = new Mock<IVerifierBroker>();
        judge.Setup(b => b.VerifyAsync(It.IsAny<string>()))
            .ReturnsAsync("1.0");

        var memory = new Mock<IMemoryBroker>();
        memory.Setup(b => b.SelectMemoriesAsync()).ReturnsAsync([]);

        var agent = new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseGenerator(generatorBroker.Object)
            .UseGate(gate.Object)
            .UseJudge(judge.Object)
            .UseMemory(memory.Object)
            .UseKnowledge(new Mock<IKnowledgeBroker>().Object)
            .UseLog(new Mock<ILogBroker>().Object);

        // when
        string actualResult = await agent.ProcessPromptAsync(prompt: "weather today?");

        // then
        actualResult.Should().Be("could not get weather");
    }

    [Fact]
    public async Task ShouldStreamResponseOnStreamPromptAsync()
    {
        // given
        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(b => b.SelectSkillsAsync()).ReturnsAsync(string.Empty);

        var memory = new Mock<IMemoryBroker>();
        memory.Setup(b => b.SelectMemoriesAsync()).ReturnsAsync([]);

        var generatorBroker = new Mock<IGeneratorBroker>();
        generatorBroker.Setup(b => b.GenerateStreamAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(ToAsyncStream("Hi ", "there."));

        var agent = new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memory.Object)
            .UseGenerator(generatorBroker.Object)
            .UseLog(new Mock<ILogBroker>().Object);

        // when
        List<AgentStreamEvent> actualEvents = [];

        await foreach (AgentStreamEvent streamEvent in agent.StreamPromptAsync(prompt: "Hi there!"))
        {
            actualEvents.Add(streamEvent);
        }

        // then
        string streamedResponse = string.Concat(actualEvents
            .Where(streamEvent => streamEvent.Type == AgentStreamEventType.Response)
            .Select(streamEvent => streamEvent.Content));

        streamedResponse.Should().Be("Hi there.");
    }

    [Fact]
    public async Task ShouldUseLocalBrainOnProcessPromptAsync()
    {
        // given
        string expectedAnswer = "answer from the local model";

        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(b => b.SelectSkillsAsync()).ReturnsAsync(string.Empty);

        var memory = new Mock<IMemoryBroker>();
        memory.Setup(b => b.SelectMemoriesAsync()).ReturnsAsync([]);

        var agent = new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memory.Object)
            .LocalBrain((systemPrompt, userPrompt) => ValueTask.FromResult(expectedAnswer))
            .UseLog(new Mock<ILogBroker>().Object);

        // when
        string actualResult = await agent.ProcessPromptAsync(prompt: "compute something");

        // then
        actualResult.Should().Be(expectedAnswer);
    }

    [Fact]
    public async Task ShouldAdvertiseOnlyDescribedToolsAtToolsMarkerOnProcessPromptAsync()
    {
        // given
        string capturedSystemPrompt = string.Empty;

        var skillBroker = new Mock<ISkillBroker>();
        skillBroker.Setup(b => b.SelectSkillsAsync()).ReturnsAsync("Tools:\n{{tools}}");

        var memory = new Mock<IMemoryBroker>();
        memory.Setup(b => b.SelectMemoriesAsync()).ReturnsAsync([]);

        var generatorBroker = new Mock<IGeneratorBroker>();
        generatorBroker.Setup(b => b.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>((systemPrompt, userPrompt) => capturedSystemPrompt = systemPrompt)
            .ReturnsAsync("FINAL: done");

        var describedTool = new Mock<Standard.Agents.Tools.ITool>();
        describedTool.SetupGet(tool => tool.Name).Returns("calculator");
        describedTool.SetupGet(tool => tool.Description).Returns("Evaluate arithmetic.");
        describedTool.SetupGet(tool => tool.Parameters).Returns("{}");

        var hiddenTool = new Mock<Standard.Agents.Tools.ITool>();
        hiddenTool.SetupGet(tool => tool.Name).Returns("secret");
        hiddenTool.SetupGet(tool => tool.Description).Returns(string.Empty);

        var agent = new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memory.Object)
            .UseGenerator(generatorBroker.Object)
            .UseLog(new Mock<ILogBroker>().Object)
            .Tool(describedTool.Object)
            .Tool(hiddenTool.Object);

        // when
        await agent.ProcessPromptAsync(prompt: "compute something");

        // then
        capturedSystemPrompt.Should().Contain("calculator");
        capturedSystemPrompt.Should().NotContain("secret");
        capturedSystemPrompt.Should().NotContain("{{tools}}");
    }

    private static async IAsyncEnumerable<string> ToAsyncStream(params string[] tokens)
    {
        foreach (string token in tokens)
        {
            await Task.Yield();

            yield return token;
        }
    }
}
