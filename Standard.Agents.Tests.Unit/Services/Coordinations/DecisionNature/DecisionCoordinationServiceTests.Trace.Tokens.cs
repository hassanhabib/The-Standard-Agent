// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Moq;
using Standard.Agents.Models.Foundations.Usages;
using Standard.Agents.Models.Orchestrations.Agents;
using Standard.Agents.Services.Coordinations.Decision;
using Standard.Agents.Services.Orchestrations.Decision.Guardians;
using Standard.Agents.Services.Orchestrations.Decision.Inferences;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Coordinations.DecisionNature;

public partial class DecisionCoordinationServiceTests
{
    // This used to assert the word "estimated" against a characters/4 figure that was logged and
    // then discarded. The figure is now the measurement the budget is enforced on, so the trace
    // records what was counted AND where the number came from — an estimate and a provider's
    // report are both usable for a bound, and only one can be reconciled against an invoice.
    [Fact]
    public async Task ShouldNarrateMeasuredTokensAndTheirProvenanceOnThinkStreamAsync()
    {
        // given
        AgentContext inputContext = CreateRandomAgentContext();
        SetupGateAllows();
        SetupJudgeApproves();

        this.usageServiceMock.Setup(service =>
            service.MeasureAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new AgentUsage(
                    PromptTokens: 12,
                    CompletionTokens: 5,
                    IsEstimated: true));

        this.brainServiceMock.Setup(service =>
            service.GenerateStreamAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(ToAsyncStream("FINAL: 42"));

        // when
        IDecisionStream decisionStream =
            this.decisionCoordinationService.ThinkStreamAsync(inputContext);

        await DrainAsync(decisionStream);

        // then — the measurement is recorded, and it says it was counted rather than reported
        this.loggingBrokerMock.Verify(broker =>
            broker.LogProcessAsync(
                "Decision",
                It.Is<string>(message =>
                    message.Contains("17 tokens") && message.Contains("counted")),
                It.IsAny<bool>()),
                    Times.Once);
    }
}
