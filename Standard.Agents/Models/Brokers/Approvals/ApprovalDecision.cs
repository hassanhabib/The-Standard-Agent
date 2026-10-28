// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Brokers.Approvals;

public enum ApprovalDecision
{
    /// <summary>Proceed: the act was authorized by a human or a trusted authority.</summary>
    Approved,

    /// <summary>Do not proceed. Non-terminal: the agent may choose another path.</summary>
    Denied,

    /// <summary>
    /// No answer yet. The turn stops with <c>AwaitingApproval</c> and the act does not run:
    /// waiting is not consent (SPEC.md section 4.9).
    /// </summary>
    Pending
}
