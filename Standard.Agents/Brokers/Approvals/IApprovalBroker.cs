// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Approvals;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Brokers.Approvals;

public interface IApprovalBroker
{
    ValueTask<ApprovalDecision> RequestAsync(AgentEffect effect);
}
