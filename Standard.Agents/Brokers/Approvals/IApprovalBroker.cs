// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Brokers.Approvals;

public enum ApprovalDecision
{
    /// <summary>Proceed — the act was authorized by a human or a trusted authority.</summary>
    Approved,

    /// <summary>Do not proceed. Non-terminal: the agent may choose another path.</summary>
    Denied,

    /// <summary>
    /// No answer yet. The turn stops with <c>AwaitingApproval</c> and the act does not run —
    /// waiting is not consent (SPEC.md §4.9).
    /// </summary>
    Pending
}

public interface IApprovalBroker
{
    ValueTask<ApprovalDecision> RequestAsync(AgentEffect effect);
}
