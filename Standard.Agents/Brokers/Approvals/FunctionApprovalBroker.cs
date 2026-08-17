// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Brokers.Approvals;

// The Custom mode's substrate: a host-supplied approver standing in for a broker — a console
// prompt, an internal service call, a test double.
public sealed class FunctionApprovalBroker : IApprovalBroker
{
    private readonly Func<AgentEffect, ValueTask<ApprovalDecision>> request;

    public FunctionApprovalBroker(Func<AgentEffect, ValueTask<ApprovalDecision>> request) =>
        this.request = request;

    public ValueTask<ApprovalDecision> RequestAsync(AgentEffect effect) =>
        this.request(effect);
}
