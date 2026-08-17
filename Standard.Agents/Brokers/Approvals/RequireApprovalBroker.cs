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
    private readonly HashSet<string> toolNamesRequiringApproval;

    public RequireApprovalBroker(IEnumerable<string> toolNamesRequiringApproval) =>
        this.toolNamesRequiringApproval =
            new HashSet<string>(toolNamesRequiringApproval, StringComparer.OrdinalIgnoreCase);

    public async ValueTask<ApprovalDecision> RequestAsync(AgentEffect effect)
    {
        await Task.CompletedTask;

        return this.toolNamesRequiringApproval.Contains(effect.ToolName)
            ? ApprovalDecision.Pending
            : ApprovalDecision.Approved;
    }
}
