// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Policies;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Services.Foundations.Policys;

// Authorization, as a foundation rather than as a broker reached from Direction.
//
// A policy engine is usually somewhere else — a rules service, an OPA sidecar, a database. When it
// is unreachable, that has to arrive as a POLICY failure. Reached directly from Direction it
// arrived as an unmapped HttpRequestException blamed on the orchestration, which is a trace that
// sends someone to read the wrong code (docs/architecture-alignment.md).
public partial class PolicyService : IPolicyService
{
    private readonly IPolicyBroker policyBroker;
    private readonly ILoggingBroker loggingBroker;

    public PolicyService(
        IPolicyBroker policyBroker,
        ILoggingBroker loggingBroker)
    {
        this.policyBroker = policyBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<AuthorizationDecision> AuthorizeEffectAsync(AgentEffect effect) =>
    TryCatch(async () =>
    {
        ValidateEffect(effect);

        // The broker's decision, unchanged. A foundation that softened a denial would be
        // deciding policy, which is not its job.
        return await this.policyBroker.AuthorizeAsync(effect);
    });
}
