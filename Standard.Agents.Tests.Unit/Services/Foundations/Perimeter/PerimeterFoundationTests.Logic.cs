// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Models.Orchestrations.Effects;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Perimeter;

public partial class PerimeterFoundationTests
{
    [Fact]
    public async Task ShouldAuthorizeEffectAsync()
    {
        // given
        AgentEffect randomEffect = CreateRandomEffect();
        AuthorizationDecision expectedDecision = AuthorizationDecision.Deny("not permitted");

        this.policyBrokerMock.Setup(broker =>
            broker.AuthorizeAsync(randomEffect))
                .ReturnsAsync(expectedDecision);

        // when — the decision must be the broker's, unchanged. A foundation that softened a
        // denial would be deciding policy, which is not its job.
        AuthorizationDecision actualDecision =
            await this.policyService.AuthorizeEffectAsync(randomEffect);

        // then
        actualDecision.Should().BeEquivalentTo(expectedDecision);

        this.policyBrokerMock.Verify(broker =>
            broker.AuthorizeAsync(randomEffect),
                Times.Once);

        this.policyBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldRequestApprovalAsync()
    {
        // given
        AgentEffect randomEffect = CreateRandomEffect();
        ApprovalDecision expectedDecision = ApprovalDecision.Pending;

        this.approvalBrokerMock.Setup(broker =>
            broker.RequestAsync(randomEffect))
                .ReturnsAsync(expectedDecision);

        // when
        ApprovalDecision actualDecision =
            await this.approvalService.RequestApprovalAsync(randomEffect);

        // then — Pending must survive the trip. Turning it into anything else here is the one
        // mistake that would let an unanswered approval look like consent.
        actualDecision.Should().Be(expectedDecision);

        this.approvalBrokerMock.Verify(broker =>
            broker.RequestAsync(randomEffect),
                Times.Once);

        this.approvalBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
