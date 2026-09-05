// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using FluentAssertions;
using Moq;
using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Models.Brokers.Effects;
using Standard.Agents.Models.Foundations.Approvals.Exceptions;
using Standard.Agents.Models.Foundations.EffectLedgers.Exceptions;
using Standard.Agents.Models.Foundations.Policys.Exceptions;
using Standard.Agents.Models.Orchestrations.Effects;
using Xunit;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Perimeter;

// The reason these foundations exist. Each resource failure must arrive named after its own
// resource — otherwise a full disk gets blamed on the orchestration that happened to ask.
public partial class PerimeterFoundationTests
{
    [Fact]
    public async Task ShouldThrowDependencyExceptionOnAuthorizeIfPolicyEngineIsUnreachableAsync()
    {
        // given
        AgentEffect randomEffect = CreateRandomEffect();
        var httpRequestException = new HttpRequestException();

        var failedPolicyDependencyException =
            new FailedPolicyDependencyException(
                message: "Failed policy dependency error occurred, contact support.",
                innerException: httpRequestException);

        var expectedPolicyDependencyException =
            new PolicyDependencyException(
                message: "Policy dependency error occurred, contact support.",
                innerException: failedPolicyDependencyException);

        this.policyBrokerMock.Setup(broker =>
            broker.AuthorizeAsync(It.IsAny<AgentEffect>()))
                .ThrowsAsync(httpRequestException);

        // when
        ValueTask<AuthorizationDecision> authorizeTask =
            this.policyService.AuthorizeEffectAsync(randomEffect);

        PolicyDependencyException actualException =
            await Assert.ThrowsAsync<PolicyDependencyException>(authorizeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedPolicyDependencyException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedPolicyDependencyException))),
                    Times.Once);

        this.policyBrokerMock.Verify(broker =>
            broker.AuthorizeAsync(randomEffect),
                Times.Once);

        this.policyBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    // The most consequential of the three. An authority that cannot be reached is a dependency
    // failure, never a verdict — a system that reads an outage as an answer will eventually read
    // it as a yes.
    [Fact]
    public async Task ShouldThrowDependencyExceptionOnRequestApprovalIfAuthorityIsUnreachableAsync()
    {
        // given
        AgentEffect randomEffect = CreateRandomEffect();
        var httpRequestException = new HttpRequestException();

        var failedApprovalDependencyException =
            new FailedApprovalDependencyException(
                message: "Failed approval dependency error occurred, contact support.",
                innerException: httpRequestException);

        var expectedApprovalDependencyException =
            new ApprovalDependencyException(
                message: "Approval dependency error occurred, contact support.",
                innerException: failedApprovalDependencyException);

        this.approvalBrokerMock.Setup(broker =>
            broker.RequestAsync(It.IsAny<AgentEffect>()))
                .ThrowsAsync(httpRequestException);

        // when
        ValueTask<ApprovalDecision> requestTask =
            this.approvalService.RequestApprovalAsync(randomEffect);

        ApprovalDependencyException actualException =
            await Assert.ThrowsAsync<ApprovalDependencyException>(requestTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedApprovalDependencyException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedApprovalDependencyException))),
                    Times.Once);

        this.approvalBrokerMock.Verify(broker =>
            broker.RequestAsync(randomEffect),
                Times.Once);

        this.approvalBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    // The failure that started the whole alignment: a full disk in the file-backed ledger,
    // surfacing unmapped and attributed to Direction.
    [Fact]
    public async Task ShouldThrowDependencyExceptionOnRecordOutcomeIfDiskIsFullAsync()
    {
        // given
        AgentEffect randomEffect = CreateRandomEffect();
        string randomOutcome = CreateRandomString();
        var ioException = new IOException();

        var failedEffectLedgerDependencyException =
            new FailedEffectLedgerDependencyException(
                message: "Failed effect ledger dependency error occurred, contact support.",
                innerException: ioException);

        var expectedEffectLedgerDependencyException =
            new EffectLedgerDependencyException(
                message: "Effect ledger dependency error occurred, contact support.",
                innerException: failedEffectLedgerDependencyException);

        this.effectLedgerBrokerMock.Setup(broker =>
            broker.UpdateRecordAsync(It.IsAny<EffectRecord>()))
                .ThrowsAsync(ioException);

        // when
        ValueTask recordTask =
            this.effectLedgerService.RecordOutcomeAsync(randomEffect, randomOutcome);

        EffectLedgerDependencyException actualException =
            await Assert.ThrowsAsync<EffectLedgerDependencyException>(recordTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedEffectLedgerDependencyException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedEffectLedgerDependencyException))),
                    Times.Once);

        this.effectLedgerBrokerMock.Verify(broker =>
            broker.SelectRecordAsync(randomEffect.IdempotencyKey),
                Times.Once);

        this.effectLedgerBrokerMock.Verify(broker =>
            broker.UpdateRecordAsync(It.IsAny<EffectRecord>()),
                Times.Once);

        this.effectLedgerBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowCriticalDependencyExceptionOnClaimEffectIfAccessDeniedAsync()
    {
        // given
        AgentEffect randomEffect = CreateRandomEffect();
        var unauthorizedAccessException = new UnauthorizedAccessException();

        this.timeBrokerMock.Setup(broker =>
            broker.GetCurrentDateTimeOffset())
                .Returns(CreateRandomDateTimeOffset());

        var failedEffectLedgerDependencyException =
            new FailedEffectLedgerDependencyException(
                message: "Failed effect ledger dependency error occurred, contact support.",
                innerException: unauthorizedAccessException);

        var expectedEffectLedgerDependencyException =
            new EffectLedgerDependencyException(
                message: "Effect ledger dependency error occurred, contact support.",
                innerException: failedEffectLedgerDependencyException);

        this.effectLedgerBrokerMock.Setup(broker =>
            broker.InsertClaimAsync(It.IsAny<EffectRecord>()))
                .ThrowsAsync(unauthorizedAccessException);

        // when
        ValueTask<EffectRecord?> claimTask =
            this.effectLedgerService.ClaimEffectAsync(randomEffect);

        EffectLedgerDependencyException actualException =
            await Assert.ThrowsAsync<EffectLedgerDependencyException>(claimTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedEffectLedgerDependencyException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogCriticalAsync(It.Is(SameExceptionAs(
                expectedEffectLedgerDependencyException))),
                    Times.Once);

        this.effectLedgerBrokerMock.Verify(broker =>
            broker.InsertClaimAsync(It.IsAny<EffectRecord>()),
                Times.Once);

        this.effectLedgerBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldThrowServiceExceptionOnAuthorizeIfServiceErrorOccursAsync()
    {
        // given
        AgentEffect randomEffect = CreateRandomEffect();
        var serviceException = new Exception();

        var failedPolicyServiceException =
            new FailedPolicyServiceException(
                message: "Failed policy service error occurred, contact support.",
                innerException: serviceException);

        var expectedPolicyServiceException =
            new PolicyServiceException(
                message: "Policy service error occurred, contact support.",
                innerException: failedPolicyServiceException);

        this.policyBrokerMock.Setup(broker =>
            broker.AuthorizeAsync(It.IsAny<AgentEffect>()))
                .ThrowsAsync(serviceException);

        // when
        ValueTask<AuthorizationDecision> authorizeTask =
            this.policyService.AuthorizeEffectAsync(randomEffect);

        PolicyServiceException actualException =
            await Assert.ThrowsAsync<PolicyServiceException>(authorizeTask.AsTask);

        // then
        actualException.Should().BeEquivalentTo(expectedPolicyServiceException);

        this.loggingBrokerMock.Verify(broker =>
            broker.LogErrorAsync(It.Is(SameExceptionAs(
                expectedPolicyServiceException))),
                    Times.Once);

        this.policyBrokerMock.Verify(broker =>
            broker.AuthorizeAsync(randomEffect),
                Times.Once);

        this.policyBrokerMock.VerifyNoOtherCalls();
        this.loggingBrokerMock.VerifyNoOtherCalls();
    }
}
