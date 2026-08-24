// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Memorys;
using Standard.Agents.Brokers.Skills;
using Standard.Agents.Brokers.Telemetries;
using Standard.Agents.Models.Foundations.Skills;
using Xunit;

namespace Standard.Agents.Tests.Unit.Clients;

// The telemetry seam's three access modes (SPEC.md §4.8): Local is .Telemetry() and emits
// through a static ActivitySource, which only an ActivityListener can observe — the broker it
// composes is covered at the broker seam here instead. External and Custom are observed
// directly, which also proves Compose() hands the broker to the loop at all.
public class StandardAgentTelemetryTests
{
    private static StandardAgent AnsweringAgent()
    {
        var skillBroker = new Mock<ISkillBroker>();

        skillBroker.Setup(broker => broker.SelectSkillsAsync())
            .ReturnsAsync(new List<Skill>());

        var memoryBroker = new Mock<IMemoryBroker>();
        memoryBroker.Setup(broker => broker.SelectMemoriesAsync()).ReturnsAsync([]);

        var knowledgeBroker = new Mock<IKnowledgeBroker>();

        knowledgeBroker.Setup(broker => broker.SelectKnowledgeAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        return new StandardAgent()
            .UseSkills(skillBroker.Object)
            .UseMemory(memoryBroker.Object)
            .UseKnowledge(knowledgeBroker.Object)
            .OnBrain(async (systemPrompt, userPrompt) => "FINAL: 42");
    }

    [Fact]
    public async Task ShouldEmitToExternalTelemetryBrokerOnProcessPromptAsync()
    {
        // given
        var telemetryBroker = new Mock<ITelemetryBroker>();

        StandardAgent agent = AnsweringAgent()
            .UseTelemetry(telemetryBroker.Object);

        // when
        await agent.ProcessPromptAsync(prompt: "what is the answer?");

        // then
        telemetryBroker.Verify(broker =>
            broker.StartRun(It.IsAny<string>()),
                Times.Once);

        telemetryBroker.Verify(broker =>
            broker.RecordRunOutcome(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
                Times.Once);
    }

    [Fact]
    public async Task ShouldEmitToCustomTelemetryDelegateOnProcessPromptAsync()
    {
        // given
        List<string> capturedEvents = [];

        StandardAgent agent = AnsweringAgent()
            .OnTelemetry((eventName, attributes) => capturedEvents.Add(eventName));

        // when
        await agent.ProcessPromptAsync(prompt: "what is the answer?");

        // then
        capturedEvents.Should().Contain("run.start");
        capturedEvents.Should().Contain("turn.start");
        capturedEvents.Should().Contain("turn.usage");
        capturedEvents.Should().Contain("run.outcome");
        capturedEvents.Should().Contain("run.end");
    }
}
