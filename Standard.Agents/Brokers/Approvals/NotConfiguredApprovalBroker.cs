// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Brokers.Approvals;

// The default. SPEC.md §4.9: absent the control, behavior is exactly as if the section did not
// exist — and a request never reaches this broker unless something explicitly asked for
// approval. RequiresApproval routes named tools to RequireApprovalBroker at composition, so the
// only path here is PermissionMode.Ask with no approver wired in: a host that said "ask about
// what nothing permitted" and gave nobody to ask. Answering Approved there was consent from
// nobody. Waiting is not consent, and an absent authority is nothing but waiting — Pending.
public sealed class NotConfiguredApprovalBroker : IApprovalBroker
{
    public ValueTask<ApprovalDecision> RequestAsync(AgentEffect effect) =>
        ValueTask.FromResult(ApprovalDecision.Pending);
}
