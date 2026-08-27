// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Tools;
using Xunit;

namespace Standard.Agents.Tests.Unit.Tools;

// A nested agent runs its own full loop, so it can end every way a run can end — answered,
// refused, held on an authority, out of turns. The outer agent sees one tool call and a string
// back, and `IAgent` returns nothing but a string, so it cannot tell WHICH of those happened.
//
// That is fine for an answer and dangerous for everything else. A sub-agent held on an approval
// returns "'wire_transfer' is waiting for approval before it can run." — prose that reads exactly
// like a result. The outer agent takes it as the tool's output, carries on, and may report the
// work as done when a human has not yet permitted it.
//
// Multi-agent is where enterprises are going, and the perimeter that Part 4 built stops at the
// boundary of a single run. This is the seam it leaks through.
public partial class AgentToolTests
{
    private static Mock<IAgent> AgentEnding(AgentStatus status, string result)
    {
        var nested = new Mock<IAgent>();

        nested.Setup(agent => agent.ProcessPromptAsync(It.IsAny<string>()))
            .ReturnsAsync(result);

        nested.Setup(agent => agent.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentOutcome(Result: result, Status: status));

        return nested;
    }

    [Fact]
    public async Task ShouldReturnTheAnswerPlainlyWhenTheNestedAgentRespondedAsync()
    {
        // given
        Mock<IAgent> nested = AgentEnding(AgentStatus.Responded, "the answer");
        var agentTool = new AgentTool(name: "researcher", agent: nested.Object);

        // when
        string actualOutput = await agentTool.ExecuteAsync("go");

        // then — an answer is an answer; nothing is added to it
        actualOutput.Should().Be("the answer");
    }

    [Theory]
    [InlineData(AgentStatus.AwaitingApproval)]
    [InlineData(AgentStatus.AwaitingInput)]
    [InlineData(AgentStatus.Refused)]
    [InlineData(AgentStatus.Failed)]
    public async Task ShouldMarkAnOutcomeThatIsNotAnAnswerAsync(AgentStatus status)
    {
        // given
        Mock<IAgent> nested = AgentEnding(
            status,
            "'wire_transfer' is waiting for approval before it can run.");

        var agentTool = new AgentTool(name: "settlement", agent: nested.Object);

        // when
        string actualOutput = await agentTool.ExecuteAsync("pay it");

        // then — the outer agent must not be able to read this as a completed result
        actualOutput.Should().StartWith(
            "[did not complete]",
            because: "a run that was held, refused or failed is not a tool result, and prose that "
                + "merely mentions waiting is indistinguishable from an answer that mentions it");

        actualOutput.Should().Contain(
            status.ToString(),
            because: "the outer agent has to know WHICH way it ended — held is not refused, and "
                + "refused is not failed");

        actualOutput.Should().Contain(
            "waiting for approval",
            because: "the inner agent's own words are what say why, and dropping them would "
                + "leave the outer agent with a category and no reason");
    }
}
