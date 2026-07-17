// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Models.Orchestrations.Agents;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Orchestrations.Decision;

public partial class DecisionOrchestrationServiceTests
{
    [Fact]
    public async Task ShouldThinkAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();
        SetupJudgeApproves();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("FINAL: 42");

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("ReturnResponse");
        actualContext.Payload.Should().Be("42");
        actualContext.RawReply.Should().Be("FINAL: 42");
    }

    // Vector 03. SPEC.md 6: a tool call MUST be read from the FIRST LINE ONLY.
    // The stray "FINAL: wrong" on line two is ignored — models emit extra lines,
    // and honouring the second one would run the wrong branch entirely.
    [Fact]
    public async Task ShouldParseActionFromFirstLineOnlyOnThinkAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("ACTION: calculator: 1+1\nFINAL: wrong");

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("calculator");
        actualContext.Payload.Should().Be("1+1");

        // The stray FINAL must not have been treated as an answer.
        actualContext.Payload.Should().NotContain("wrong");
    }

    // Vector 04. SPEC.md 6: a final answer MAY span multiple lines. Trim must not
    // collapse the internal newline.
    [Fact]
    public async Task ShouldPreserveMultilineFinalOnThinkAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();
        SetupJudgeApproves();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("FINAL: line one\nline two");

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("ReturnResponse");
        actualContext.Payload.Should().Be("line one\nline two");
    }

    // SPEC.md 6 gives no escaping rule for a colon inside the tool input. Splitting
    // on the FIRST colon only keeps a URL intact; splitting on every colon would
    // hand the tool "http" and lose the rest.
    [Fact]
    public async Task ShouldPreserveColonsInToolInputOnThinkAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("ACTION: fetch: https://example.com:8080/a?b=1");

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("fetch");
        actualContext.Payload.Should().Be("https://example.com:8080/a?b=1");
    }

    // Hassan's decision on #33: intent mirrors the parsed action — the tool name for
    // an ACTION, "Respond" for a FINAL, "Refuse" when the Gate refuses. Derived from
    // the reply the Brain already gave; no extra inference call.
    [Fact]
    public async Task ShouldSetIntentToToolNameOnThinkIfActionAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("ACTION: calculator: 1+1");

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.Intent.Should().Be("calculator");
    }

    [Fact]
    public async Task ShouldSetIntentToRespondOnThinkIfFinalAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();
        SetupJudgeApproves();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("FINAL: 42");

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.Intent.Should().Be("Respond");
    }

    // A model that forgets the protocol still gets its answer through. Treating an
    // unprefixed reply as a FINAL is the forgiving reading; the alternative is an
    // agent that refuses to answer because the model omitted four characters.
    [Fact]
    public async Task ShouldTreatNonProtocolReplyAsAnswerOnThinkAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();
        SetupJudgeApproves();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("just an answer, no prefix");

        // when
        AgentContext actualContext =
            await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        actualContext.DirectionType.Should().Be("ReturnResponse");
        actualContext.Payload.Should().Be("just an answer, no prefix");
    }

    // Observations reach the Brain — SPEC.md 5 requires the next Decision be able to
    // read what Direction fed back. Without this, vector 02 cannot pass: the tool
    // result would exist on the context and never reach the mind that needs it.
    [Fact]
    public async Task ShouldPassObservationsToBrainOnThinkAsync()
    {
        // given
        string observation = CreateRandomString();

        AgentContext inputContext = new()
        {
            Prompt = CreateRandomString(),
            SystemPrompt = CreateRandomString(),
            Observations = [observation]
        };

        SetupGateAllows();
        SetupJudgeApproves();

        this.brainServiceMock.Setup(service =>
            service.GenerateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("FINAL: done");

        // when
        await this.decisionOrchestrationService.ThinkAsync(inputContext);

        // then
        this.brainServiceMock.Verify(service =>
            service.GenerateAsync(
                inputContext.SystemPrompt,
                It.Is<string>(userMessage => userMessage.Contains(observation))),
                    Times.Once);
    }
}
