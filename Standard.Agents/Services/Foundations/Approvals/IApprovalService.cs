// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Approvals;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Services.Foundations.Approvals;

public interface IApprovalService
{
    /// <summary>
    /// Asks an authority to permit this act. <c>Pending</c> is not consent (SPEC.md §4.9).
    /// </summary>
    ValueTask<ApprovalDecision> RequestApprovalAsync(AgentEffect effect);
}
