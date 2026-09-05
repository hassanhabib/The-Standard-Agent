// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Linq.Expressions;
using Moq;
using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Brokers.Effects;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Policies;
using Standard.Agents.Brokers.Times;
using Standard.Agents.Models.Orchestrations.Effects;
using Standard.Agents.Services.Foundations.Approvals;
using Standard.Agents.Services.Foundations.EffectLedgers;
using Standard.Agents.Services.Foundations.Policys;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Standard.Agents.Tests.Unit.Services.Foundations.Perimeter;

// The three perimeter foundations share one job: turn a resource failure into a failure named
// after the resource. They are tested together because that is the property, and it is the same
// property three times.
public partial class PerimeterFoundationTests
{
    private readonly Mock<IPolicyBroker> policyBrokerMock;
    private readonly Mock<IApprovalBroker> approvalBrokerMock;
    private readonly Mock<IEffectLedgerBroker> effectLedgerBrokerMock;
    private readonly Mock<ITimeBroker> timeBrokerMock;
    private readonly Mock<ILoggingBroker> loggingBrokerMock;

    private readonly IPolicyService policyService;
    private readonly IApprovalService approvalService;
    private readonly IEffectLedgerService effectLedgerService;

    public PerimeterFoundationTests()
    {
        this.policyBrokerMock = new Mock<IPolicyBroker>();
        this.approvalBrokerMock = new Mock<IApprovalBroker>();
        this.effectLedgerBrokerMock = new Mock<IEffectLedgerBroker>();
        this.timeBrokerMock = new Mock<ITimeBroker>();
        this.loggingBrokerMock = new Mock<ILoggingBroker>();

        this.policyService = new PolicyService(
            policyBroker: this.policyBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);

        this.approvalService = new ApprovalService(
            approvalBroker: this.approvalBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);

        this.effectLedgerService = new EffectLedgerService(
            effectLedgerBroker: this.effectLedgerBrokerMock.Object,
            timeBroker: this.timeBrokerMock.Object,
            loggingBroker: this.loggingBrokerMock.Object);
    }

    private static string CreateRandomString() =>
        new MnemonicString().GetValue();

    private static AgentEffect CreateRandomEffect() =>
        AgentEffect.For(
            runId: CreateRandomString(),
            toolName: CreateRandomString(),
            arguments: CreateRandomString());

    private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
        actualException => actualException.SameExceptionAs(expectedException);
}
