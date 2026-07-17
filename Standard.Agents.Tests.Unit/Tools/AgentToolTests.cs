// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Logs;
using Standard.Agents.Tools;
using Tynamix.ObjectFiller;
using Xunit;

namespace Standard.Agents.Tests.Unit.Tools;

public class AgentToolTests
{
    private static string CreateRandomString() =>
        new MnemonicString().GetValue();

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
            agent.ProcessPromptAsync(randomInput))
                .ReturnsAsync(randomAnswer);

        var agentTool = new AgentTool(randomName, nestedAgentMock.Object);

        // when
        string actualOutput = await agentTool.ExecuteAsync(randomInput);

        // then
        actualOutput.Should().Be(expectedOutput);
        agentTool.Name.Should().Be(randomName);

                nestedAgentMock.Verify(agent =>
            agent.ProcessPromptAsync(randomInput),
                Times.Once);

        nestedAgentMock.VerifyNoOtherCalls();
    }

                [Fact]
    public async Task ShouldNestAgentInsideAgentAsync()
    {
        // given — the inner agent answers anything with a fixed finding
        var innerAgent = new Mock<IAgent>();

        innerAgent.Setup(agent =>
            agent.ProcessPromptAsync(It.IsAny<string>()))
                .ReturnsAsync("the capital is Paris");

        var researcher = new AgentTool("researcher", innerAgent.Object);

                var outerBrain = new Mock<Brokers.Generators.IGeneratorBroker>();
        var replies = new Queue<string>(
        [
            "ACTION: researcher: capital of France",
            "FINAL: Paris"
        ]);

        outerBrain.Setup(broker =>
            broker.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(() => replies.Dequeue());

        var skills = new Mock<Brokers.Skills.ISkillBroker>();
        skills.Setup(b => b.SelectSkillsAsync()).ReturnsAsync("you are an agent");

        var memory = new Mock<Brokers.Memorys.IMemoryBroker>();
        memory.Setup(b => b.SelectMemoriesAsync()).ReturnsAsync([]);

        var gate = new Mock<Brokers.Classifiers.IClassifierBroker>();
        gate.Setup(b => b.ClassifyAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("allow");

        var judge = new Mock<Brokers.Verifiers.IVerifierBroker>();
        judge.Setup(b => b.VerifyAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(1.0);

        var outerAgent = new StandardAgent()
            .UseSkills(skills.Object)
            .UseGenerator(outerBrain.Object)
            .UseMemory(memory.Object)
            .UseKnowledge(new Mock<Brokers.Knowledges.IKnowledgeBroker>().Object)
            .UseGate(gate.Object)
            .UseJudge(judge.Object)
            .UseMcp(new Mock<Brokers.Mcps.IMcpBroker>().Object)
            .UseLog(new Mock<Brokers.Logs.ILogBroker>().Object)
            .Tool(researcher);

        // when
        string actualResult = await outerAgent.ProcessPromptAsync("what is the capital of France?");

        // then
        actualResult.Should().Be("Paris");

                innerAgent.Verify(agent =>
            agent.ProcessPromptAsync("capital of France"),
                Times.Once);
    }

                [Fact]
    public async Task ShouldPropagateOnExecuteIfNestedAgentThrowsAsync()
    {
        // given
        var nestedAgentMock = new Mock<IAgent>();
        var nestedFailure = new InvalidOperationException("inner agent failed");

        nestedAgentMock.Setup(agent =>
            agent.ProcessPromptAsync(It.IsAny<string>()))
                .ThrowsAsync(nestedFailure);

        var agentTool = new AgentTool("nested", nestedAgentMock.Object);

        // when
        ValueTask<string> executeTask = agentTool.ExecuteAsync("anything");

        InvalidOperationException actualException =
            await Assert.ThrowsAsync<InvalidOperationException>(
                executeTask.AsTask);

        // then
        actualException.Message.Should().Be("inner agent failed");
    }
}
