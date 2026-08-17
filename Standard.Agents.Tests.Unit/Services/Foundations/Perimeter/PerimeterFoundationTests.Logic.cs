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

    [Fact]
    public async Task ShouldRetrieveOutcomeAsync()
    {
        // given
        AgentEffect randomEffect = CreateRandomEffect();
        string randomOutcome = CreateRandomString();
        string expectedOutcome = randomOutcome;

        this.effectLedgerBrokerMock.Setup(broker =>
            broker.SelectOutcomeAsync(randomEffect))
                .ReturnsAsync(expectedOutcome);

        // when
        string? actualOutcome =
            await this.effectLedgerService.RetrieveOutcomeAsync(randomEffect);

        // then
        actualOutcome.Should().Be(expectedOutcome);

        this.effectLedgerBrokerMock.Verify(broker =>
            broker.SelectOutcomeAsync(randomEffect),
                Times.Once);

        this.effectLedgerBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    // An act that has not run yet returns null, and that has to stay distinct from a read that
    // failed — one means proceed, the other means stop.
    [Fact]
    public async Task ShouldRetrieveNoOutcomeOnRetrieveOutcomeIfActIsNewAsync()
    {
        // given
        AgentEffect randomEffect = CreateRandomEffect();

        this.effectLedgerBrokerMock.Setup(broker =>
            broker.SelectOutcomeAsync(randomEffect))
                .ReturnsAsync((string?)null);

        // when
        string? actualOutcome =
            await this.effectLedgerService.RetrieveOutcomeAsync(randomEffect);

        // then
        actualOutcome.Should().BeNull();

        this.effectLedgerBrokerMock.Verify(broker =>
            broker.SelectOutcomeAsync(randomEffect),
                Times.Once);

        this.effectLedgerBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldRecordOutcomeAsync()
    {
        // given
        AgentEffect randomEffect = CreateRandomEffect();
        string randomOutcome = CreateRandomString();

        // when
        await this.effectLedgerService.RecordOutcomeAsync(randomEffect, randomOutcome);

        // then
        this.effectLedgerBrokerMock.Verify(broker =>
            broker.InsertOutcomeAsync(randomEffect, randomOutcome),
                Times.Once);

        this.effectLedgerBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldReleaseClaimAsync()
    {
        // given
        AgentEffect randomEffect = CreateRandomEffect();

        // when
        await this.effectLedgerService.ReleaseClaimAsync(randomEffect);

        // then
        this.effectLedgerBrokerMock.Verify(broker =>
            broker.DeleteClaimAsync(randomEffect),
                Times.Once);

        this.effectLedgerBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
