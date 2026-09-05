// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Effects;

namespace Standard.Agents.Brokers.Effects;

/// <summary>
/// The store of which acts have run (SPEC.md §4.9), as four primitives over typed records. What a
/// record means, and what to do about one that is already there, is the foundation's judgment
/// (principal review 2026-09-04, F-08); this broker only keeps the records.
/// </summary>
public interface IEffectLedgerBroker
{
    /// <summary>
    /// Writes the claim if, and only if, no record exists for its key — atomically, so two runs
    /// proposing the same act cannot both decide they are the first. Intent is written before
    /// the act, never after.
    /// </summary>
    /// <returns><c>true</c> when the claim was written; <c>false</c> when the key already had a record.</returns>
    ValueTask<bool> InsertClaimAsync(EffectRecord claim);

    ValueTask<EffectRecord?> SelectRecordAsync(string idempotencyKey);

    /// <summary>Replaces the key's record with this one: the act's outcome, its failure, its compensation.</summary>
    ValueTask UpdateRecordAsync(EffectRecord record);

    ValueTask DeleteRecordAsync(string idempotencyKey);
}
