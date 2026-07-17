// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
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

        // The tool's input becomes the nested agent's PROMPT — that is the bridge.
        nestedAgentMock.Verify(agent =>
            agent.ProcessPromptAsync(randomInput),
                Times.Once);

        nestedAgentMock.VerifyNoOtherCalls();
    }

    // ⭐ The fractal, actually exercised: an agent registered as a tool of another
    // agent. The outer agent asks for "researcher" and cannot tell whether a function
    // or a whole mind answered — which is the point of Theory Ch.4.
    [Fact]
    public async Task ShouldNestAgentInsideAgentAsync()
    {
        // given — the inner agent answers anything with a fixed finding
        var innerAgent = new Mock<IAgent>();

        innerAgent.Setup(agent =>
            agent.ProcessPromptAsync(It.IsAny<string>()))
                .ReturnsAsync("the capital is Paris");

        var researcher = new AgentTool("researcher", innerAgent.Object);

        // the outer agent uses it once, then answers
        var outerBrain = new Mock<Brokers.Decision.IGeneratorBroker>();
        var replies = new Queue<string>(
        [
            "ACTION: researcher: capital of France",
            "FINAL: Paris"
        ]);

        outerBrain.Setup(broker =>
            broker.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(() => replies.Dequeue());

        var skills = new Mock<Brokers.Data.ISkillBroker>();
        skills.Setup(b => b.SelectSkillsAsync()).ReturnsAsync("you are an agent");

        var memory = new Mock<Brokers.Data.IMemoryBroker>();
        memory.Setup(b => b.SelectMemoriesAsync()).ReturnsAsync([]);

        var gate = new Mock<Brokers.Decision.IClassifierBroker>();
        gate.Setup(b => b.ClassifyAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("allow");

        var judge = new Mock<Brokers.Decision.IVerifierBroker>();
        judge.Setup(b => b.VerifyAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(1.0);

        var outerAgent = new StandardAgent()
            .UseSkills(skills.Object)
            .UseGenerator(outerBrain.Object)
            .UseMemory(memory.Object)
            .UseKnowledge(new Mock<Brokers.Data.IKnowledgeBroker>().Object)
            .UseGate(gate.Object)
            .UseJudge(judge.Object)
            .UseMcp(new Mock<Brokers.Direction.IMcpBroker>().Object)
            .UseLog(new Mock<Brokers.Loggings.ILogBroker>().Object)
            .Tool(researcher);

        // when
        string actualResult = await outerAgent.ProcessPromptAsync("what is the capital of France?");

        // then
        actualResult.Should().Be("Paris");

        // The nested agent ran its own loop, driven by the outer agent's tool call.
        innerAgent.Verify(agent =>
            agent.ProcessPromptAsync("capital of France"),
                Times.Once);
    }

    // A nested agent that fails is a tool that fails. The exception is not swallowed
    // into a plausible-looking answer — the outer agent's Direction categorises it
    // like any other dependency failure.
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
