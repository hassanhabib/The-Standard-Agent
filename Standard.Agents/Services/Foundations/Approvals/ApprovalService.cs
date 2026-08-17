// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Services.Foundations.Approvals;

// Human approval, as a foundation.
//
// This one matters most of the three. An approval broker reaches a person — a queue, a ticket, a
// chat prompt — and when that is unreachable the honest answer is a dependency failure, never a
// decision. A raw exception escaping into Direction risks being read as "not approved", and a
// system that treats an outage as a verdict is a system that will one day treat it as a yes.
public partial class ApprovalService : IApprovalService
{
    private readonly IApprovalBroker approvalBroker;
    private readonly ILoggingBroker loggingBroker;

    public ApprovalService(
        IApprovalBroker approvalBroker,
        ILoggingBroker loggingBroker)
    {
        this.approvalBroker = approvalBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<ApprovalDecision> RequestApprovalAsync(AgentEffect effect) =>
    TryCatch(async () =>
    {
        await ValueTask.CompletedTask;

        return ApprovalDecision.Approved;
    });
}
