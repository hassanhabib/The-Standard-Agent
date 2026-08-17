// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Brokers.Approvals;

// The default. SPEC.md §4.9: absent the control, behavior is exactly as if the section did not
// exist — nothing is held for approval.
public sealed class NotConfiguredApprovalBroker : IApprovalBroker
{
    public ValueTask<ApprovalDecision> RequestAsync(AgentEffect effect) =>
        ValueTask.FromResult(ApprovalDecision.Approved);
}
