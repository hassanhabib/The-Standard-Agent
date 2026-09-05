// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Effects;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Times;
using Standard.Agents.Models.Brokers.Effects;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Services.Foundations.EffectLedgers;

public partial class EffectLedgerService : IEffectLedgerService
{
    // How long an in-flight claim is presumed live. Past it, a claim with no outcome is an
    // earlier attempt whose fate is unknown, and a repeat is held for reconciliation rather than
    // presumed either way (principal review 2026-09-04, F-08).
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(5);

    private readonly IEffectLedgerBroker effectLedgerBroker;
    private readonly ITimeBroker timeBroker;
    private readonly ILoggingBroker loggingBroker;

    public EffectLedgerService(
        IEffectLedgerBroker effectLedgerBroker,
        ITimeBroker timeBroker,
        ILoggingBroker loggingBroker)
    {
        this.effectLedgerBroker = effectLedgerBroker;
        this.timeBroker = timeBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<EffectRecord?> ClaimEffectAsync(AgentEffect effect) =>
    TryCatch(async () =>
    {
        ValidateEffect(effect);
        DateTimeOffset now = this.timeBroker.GetCurrentDateTimeOffset();

        var claim = new EffectRecord
        {
            IdempotencyKey = effect.IdempotencyKey,
            ToolName = effect.ToolName,
            State = EffectState.InFlight,
            Owner = effect.RunId,
            ClaimedOn = now,
            LeaseUntil = now + Lease
        };

        bool claimed = await this.effectLedgerBroker.InsertClaimAsync(claim);

        // Null means this act has not run and the caller should proceed. It has to stay distinct
        // from a read that failed: one means go, the other means stop.
        return claimed
            ? null
            : await this.effectLedgerBroker.SelectRecordAsync(effect.IdempotencyKey);
    });

    public ValueTask RecordOutcomeAsync(AgentEffect effect, string outcome) =>
    TryCatch(async () =>
    {
        ValidateEffect(effect);

        await UpdateAsync(
            effect.IdempotencyKey,
            effect.ToolName,
            effect.RunId,
            EffectState.Completed,
            outcome: outcome,
            detail: null);
    });

    public ValueTask RecordFailureAsync(AgentEffect effect, string detail) =>
    TryCatch(async () =>
    {
        ValidateEffect(effect);

        await UpdateAsync(
            effect.IdempotencyKey,
            effect.ToolName,
            effect.RunId,
            EffectState.Failed,
            outcome: null,
            detail: detail);
    });

    // Only an unfinished claim is given back. A record with an outcome, a failure or a
    // compensation names an act that happened, and forgetting that would turn run-once back
    // into run-again.
    public ValueTask ReleaseClaimAsync(AgentEffect effect) =>
    TryCatch(async () =>
    {
        ValidateEffect(effect);

        EffectRecord? record =
            await this.effectLedgerBroker.SelectRecordAsync(effect.IdempotencyKey);

        if (record?.State is EffectState.InFlight)
        {
            await this.effectLedgerBroker.DeleteRecordAsync(effect.IdempotencyKey);
        }
    });

    public ValueTask RecordCompensationIntentAsync(string idempotencyKey) =>
    TryCatch(async () =>
    {
        ValidateKey(idempotencyKey);

        EffectRecord? record = await this.effectLedgerBroker.SelectRecordAsync(idempotencyKey);
        ValidateRecordExists(record);

        await this.effectLedgerBroker.UpdateRecordAsync(record! with
        {
            State = EffectState.CompensationPending,
            RecordedOn = this.timeBroker.GetCurrentDateTimeOffset()
        });
    });

    public ValueTask RecordCompensationAsync(string idempotencyKey, string detail) =>
    TryCatch(async () =>
    {
        ValidateKey(idempotencyKey);

        EffectRecord? record = await this.effectLedgerBroker.SelectRecordAsync(idempotencyKey);
        ValidateRecordExists(record);

        await this.effectLedgerBroker.UpdateRecordAsync(record! with
        {
            State = EffectState.Compensated,
            Detail = detail,
            RecordedOn = this.timeBroker.GetCurrentDateTimeOffset()
        });
    });

    // The record is completed in place when the claim is there, and written whole when it is
    // not: a ledger that lost the claim still gets the outcome, which is the fact that matters.
    private async ValueTask UpdateAsync(
        string idempotencyKey,
        string toolName,
        string owner,
        EffectState state,
        string? outcome,
        string? detail)
    {
        DateTimeOffset now = this.timeBroker.GetCurrentDateTimeOffset();

        EffectRecord record =
            await this.effectLedgerBroker.SelectRecordAsync(idempotencyKey)
                ?? new EffectRecord
                {
                    IdempotencyKey = idempotencyKey,
                    ToolName = toolName,
                    Owner = owner,
                    ClaimedOn = now,
                    LeaseUntil = now
                };

        await this.effectLedgerBroker.UpdateRecordAsync(record with
        {
            State = state,
            Outcome = outcome,
            Detail = detail,
            RecordedOn = now
        });
    }
}
