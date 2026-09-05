// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Collections.Concurrent;
using Standard.Agents.Models.Brokers.Effects;

namespace Standard.Agents.Brokers.Effects;

// The built-in ledger: one process, one lifetime. Run-once holds within the instance; nothing
// here survives it, which is what .EffectLedger(path) and UseEffectLedger(...) are for.
public sealed class InMemoryEffectLedgerBroker : IEffectLedgerBroker
{
    private readonly ConcurrentDictionary<string, EffectRecord> recordsByKey = new();

    public async ValueTask<bool> InsertClaimAsync(EffectRecord claim) =>
        this.recordsByKey.TryAdd(claim.IdempotencyKey, claim);

    public async ValueTask<EffectRecord?> SelectRecordAsync(string idempotencyKey) =>
        this.recordsByKey.GetValueOrDefault(idempotencyKey);

    public async ValueTask UpdateRecordAsync(EffectRecord record) =>
        this.recordsByKey[record.IdempotencyKey] = record;

    public async ValueTask DeleteRecordAsync(string idempotencyKey) =>
        this.recordsByKey.TryRemove(idempotencyKey, out _);
}
