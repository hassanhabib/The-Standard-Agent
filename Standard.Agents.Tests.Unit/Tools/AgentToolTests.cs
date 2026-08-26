// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Foundations.Skills;
using Standard.Agents.Models.Orchestrations.Agents;
using FluentAssertions;
using Moq;
using Standard.Agents.Tools;
using Tynamix.ObjectFiller;
using Xunit;
using Standard.Agents.Models.Brokers.Generators;

namespace Standard.Agents.Tests.Unit.Tools;

public partial class AgentToolTests
{
    private static string CreateRandomString() =>
        new MnemonicString().GetValue();

    private static Brokers.Knowledges.IKnowledgeBroker EmptyKnowledgeBroker()
    {
        var knowledgeBroker = new Mock<Brokers.Knowledges.IKnowledgeBroker>();
        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>())).ReturnsAsync([]);

        return knowledgeBroker.Object;
    }

    [Fact]
    public async Task ShouldRunNestedAgentAsToolAsync()
    {
        // given
        string randomName = CreateRandomString();
        string randomInput = CreateRandomString();
        string randomAnswer = CreateRandomString();
        string expectedOutput = randomAnswer;

        var nestedAgentMock = new Mock<IAgent>();

        nestedAgentMock.Setup(agent =>
            agent.RunAsync(randomInput))
                .ReturnsAsync(new AgentOutcome(randomAnswer, AgentStatus.Responded));

        var agentTool = new AgentTool(name: randomName, agent: nestedAgentMock.Object);

        // when
        string actualOutput = await agentTool.ExecuteAsync(randomInput);

        // then
        actualOutput.Should().Be(expectedOutput);
        agentTool.Name.Should().Be(randomName);

        nestedAgentMock.Verify(agent =>
    agent.RunAsync(randomInput),
        Times.Once);

        nestedAgentMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldNestAgentInsideAgentAsync()
    {
        // given — the inner agent answers anything with a fixed finding
        var innerAgent = new Mock<IAgent>();

        innerAgent.Setup(agent =>
            agent.RunAsync(It.IsAny<string>()))
                .ReturnsAsync(new AgentOutcome("the capital is Paris", AgentStatus.Responded));

        var researcher = new AgentTool(name: "researcher", agent: innerAgent.Object);

        var outerBrain = new Mock<Brokers.Generators.IGeneratorBroker>();
        var replies = new Queue<string>(
        [
            "ACTION: researcher: capital of France",
            "FINAL: Paris"
        ]);

        outerBrain.Setup(broker =>
            broker.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(() => replies.Dequeue());

        outerBrain.Setup(broker => broker.GenerateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ResolvedInference>()))
                .Returns((string systemPrompt, string userPrompt, ResolvedInference _) =>
                    outerBrain.Object.GenerateAsync(systemPrompt, userPrompt));

        var skills = new Mock<Brokers.Skills.ISkillBroker>();
        skills.Setup(broker => broker.SelectSkillsAsync()).ReturnsAsync(new List<Skill> { new() { Content = "you are an agent" } });

        var memory = new Mock<Brokers.Memorys.IMemoryBroker>();
        memory.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var gate = new Mock<Brokers.Classifiers.IClassifierBroker>();
        gate.Setup(broker => broker.ClassifyAsync(It.IsAny<string>()))
            .ReturnsAsync("allow");

        var judge = new Mock<Brokers.Verifiers.IVerifierBroker>();
        judge.Setup(broker => broker.VerifyAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("1.0");

        var outerAgent = new StandardAgent()
            .UseSkills(skills.Object)
            .UseGenerator(outerBrain.Object)
            .UseMemory(memory.Object)
            .UseKnowledge(EmptyKnowledgeBroker())
            .UseGate(gate.Object)
            .UseJudge(judge.Object)
            .UseMcp(new Mock<Brokers.Mcps.IMcpBroker>().Object)
            .UseLogging(new Mock<Standard.Agents.Brokers.Loggings.ILoggingBroker>().Object)
            .Tool(researcher);

        // when
        string actualResult = await outerAgent.ProcessPromptAsync(prompt: "what is the capital of France?");

        // then
        actualResult.Should().Be("Paris");

        innerAgent.Verify(agent =>
    agent.RunAsync("capital of France"),
        Times.Once);
    }

    [Fact]
    public async Task ShouldPropagateOnExecuteIfNestedAgentThrowsAsync()
    {
        // given
        var nestedAgentMock = new Mock<IAgent>();
        var nestedFailure = new InvalidOperationException(message: "inner agent failed");

        nestedAgentMock.Setup(agent =>
            agent.RunAsync(It.IsAny<string>()))
                .ThrowsAsync(nestedFailure);

        var agentTool = new AgentTool(name: "nested", agent: nestedAgentMock.Object);

        // when
        ValueTask<string> executeTask = agentTool.ExecuteAsync(input: "anything");

        InvalidOperationException actualException =
            await Assert.ThrowsAsync<InvalidOperationException>(
                executeTask.AsTask);

        // then
        actualException.Message.Should().Be("inner agent failed");
    }

    [Fact]
    public async Task ShouldApplyHandoffTemplateOnExecuteAsync()
    {
        // given
        string randomInput = CreateRandomString();
        string randomAnswer = CreateRandomString();
        string handoff = "You are a researcher. Research this and report: {input}";
        string expectedHandoffPrompt = $"You are a researcher. Research this and report: {randomInput}";

        var nestedAgentMock = new Mock<IAgent>();

        nestedAgentMock.Setup(agent =>
            agent.RunAsync(expectedHandoffPrompt))
                .ReturnsAsync(new AgentOutcome(randomAnswer, AgentStatus.Responded));

        var agentTool = new AgentTool(
            name: "researcher",
            agent: nestedAgentMock.Object,
            handoff: handoff);

        // when
        string actualOutput = await agentTool.ExecuteAsync(randomInput);

        // then
        actualOutput.Should().Be(randomAnswer);

        nestedAgentMock.Verify(agent =>
            agent.RunAsync(expectedHandoffPrompt),
                Times.Once);

        nestedAgentMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void ShouldExposeDescriptionAndParametersOnAgentTool()
    {
        // given
        string description = "Delegates a research question to a specialist.";
        string parameters = "{\"query\":\"string\"}";

        var agentTool = new AgentTool(
            name: "researcher",
            agent: new Mock<IAgent>().Object,
            description: description,
            parameters: parameters);

        // then
        agentTool.Description.Should().Be(description);
        agentTool.Parameters.Should().Be(parameters);
    }
}
