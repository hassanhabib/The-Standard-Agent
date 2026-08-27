// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Brokers.Approvals;

// The Local mode: a list of tool names that always need someone else to say yes. With no
// approver wired in, the honest answer is Pending — the act is held, not performed. An agent
// that treated "nobody answered" as consent would be worse than one with no approval at all,
// because the control would read as present while granting everything.
public sealed class RequireApprovalBroker : IApprovalBroker
{
    // The list's work happens in composition, where it decides WHICH acts require approval at
    // all (DirectionCoordinationService.RequiresApproval). By the time a request reaches this
    // broker the question is always "may this act run?" — asked for a listed tool, or for an
    // act the Ask mode says nothing permitted — and with no approver wired in, the honest
    // answer to both is the same. Answering Approved for a tool absent from the list was
    // consent from nobody: that branch was reachable only under Ask, where it ran acts the
    // authority was never told about.
    public RequireApprovalBroker(IEnumerable<string> toolNamesRequiringApproval)
    {
    }

    public async ValueTask<ApprovalDecision> RequestAsync(AgentEffect effect)
    {
        await Task.CompletedTask;

        return ApprovalDecision.Pending;
    }
}
