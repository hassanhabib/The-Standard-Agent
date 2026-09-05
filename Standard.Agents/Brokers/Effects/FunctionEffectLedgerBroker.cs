// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Effects;

namespace Standard.Agents.Brokers.Effects;

// The Custom mode of the ledger: the four primitives as your own delegates, usually over the
// store your transactions already commit to, since an act and the note that it happened want
// to commit together.
public sealed class FunctionEffectLedgerBroker : IEffectLedgerBroker
{
    private readonly Func<EffectRecord, ValueTask<bool>> insertClaim;
    private readonly Func<string, ValueTask<EffectRecord?>> selectRecord;
    private readonly Func<EffectRecord, ValueTask> updateRecord;
    private readonly Func<string, ValueTask> deleteRecord;

    public FunctionEffectLedgerBroker(
        Func<EffectRecord, ValueTask<bool>> insertClaim,
        Func<string, ValueTask<EffectRecord?>> selectRecord,
        Func<EffectRecord, ValueTask> updateRecord,
        Func<string, ValueTask> deleteRecord)
    {
        this.insertClaim = insertClaim;
        this.selectRecord = selectRecord;
        this.updateRecord = updateRecord;
        this.deleteRecord = deleteRecord;
    }

    public ValueTask<bool> InsertClaimAsync(EffectRecord claim) =>
        this.insertClaim(claim);

    public ValueTask<EffectRecord?> SelectRecordAsync(string idempotencyKey) =>
        this.selectRecord(idempotencyKey);

    public ValueTask UpdateRecordAsync(EffectRecord record) =>
        this.updateRecord(record);

    public ValueTask DeleteRecordAsync(string idempotencyKey) =>
        this.deleteRecord(idempotencyKey);
}
