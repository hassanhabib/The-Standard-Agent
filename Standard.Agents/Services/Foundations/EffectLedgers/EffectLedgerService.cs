// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Effects;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Services.Foundations.EffectLedgers;

// Run-once, as a foundation.
//
// This is the one that motivated the whole alignment: a full disk in the file-backed ledger
// surfaced as a raw IOException and got attributed to Direction, so the trace blamed the
// orchestration for a storage failure (docs/architecture-alignment.md).
public partial class EffectLedgerService : IEffectLedgerService
{
    private readonly IEffectLedgerBroker effectLedgerBroker;
    private readonly ILoggingBroker loggingBroker;

    public EffectLedgerService(
        IEffectLedgerBroker effectLedgerBroker,
        ILoggingBroker loggingBroker)
    {
        this.effectLedgerBroker = effectLedgerBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<string?> RetrieveOutcomeAsync(AgentEffect effect) =>
    TryCatch(async () =>
    {
        ValidateEffect(effect);

        // Null means this act has not run and the caller should proceed. It has to stay distinct
        // from a read that failed: one means go, the other means stop.
        return await this.effectLedgerBroker.SelectOutcomeAsync(effect);
    });

    public ValueTask RecordOutcomeAsync(AgentEffect effect, string outcome) =>
    TryCatch(async () =>
    {
        ValidateEffect(effect);

        await this.effectLedgerBroker.InsertOutcomeAsync(effect, outcome);
    });

    public ValueTask ReleaseClaimAsync(AgentEffect effect) =>
    TryCatch(async () =>
    {
        ValidateEffect(effect);

        await this.effectLedgerBroker.DeleteClaimAsync(effect);
    });
}
