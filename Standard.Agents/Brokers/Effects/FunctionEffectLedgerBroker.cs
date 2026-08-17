// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Brokers.Effects;

// The Custom mode's substrate: host-supplied claim and record, for the store the host already
// runs its transactions in — which is usually where run-once belongs, since an effect and the
// note that it happened want to commit together.
public sealed class FunctionEffectLedgerBroker : IEffectLedgerBroker
{
    private readonly Func<AgentEffect, ValueTask<string?>> claim;
    private readonly Func<AgentEffect, string, ValueTask> record;

    public FunctionEffectLedgerBroker(
        Func<AgentEffect, ValueTask<string?>> claim,
        Func<AgentEffect, string, ValueTask> record)
    {
        this.claim = claim;
        this.record = record;
    }

    public ValueTask<string?> SelectOutcomeAsync(AgentEffect effect) =>
        this.claim(effect);

    public ValueTask InsertOutcomeAsync(AgentEffect effect, string outcome) =>
        this.record(effect, outcome);
}
