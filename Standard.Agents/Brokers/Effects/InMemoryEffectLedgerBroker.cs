// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Collections.Concurrent;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Brokers.Effects;

// Run-once within one agent instance. That is the whole scope, and saying so is the point:
// it covers a retry inside a run and a repeat proposal across turns, which is where duplicate
// effects actually come from today. It does NOT survive a process restart — cross-process
// run-once needs a durable ledger, which arrives with the session store.
//
// A host that needs the stronger guarantee now swaps this broker for one over its own store;
// that is what the seam is for (SPEC.md §4.1).
public sealed class InMemoryEffectLedgerBroker : IEffectLedgerBroker
{
    private readonly ConcurrentDictionary<string, string?> outcomesByKey = new();

    public ValueTask<string?> SelectOutcomeAsync(AgentEffect effect)
    {
        // Claim the key and report the prior outcome in one step, so two concurrent runs
        // proposing the same act cannot both decide they are the first.
        bool isFirst = this.outcomesByKey.TryAdd(effect.IdempotencyKey, null);

        if (isFirst)
        {
            return ValueTask.FromResult<string?>(null);
        }

        this.outcomesByKey.TryGetValue(effect.IdempotencyKey, out string? outcome);

        return ValueTask.FromResult<string?>(outcome ?? "effect already in progress");
    }

    public ValueTask InsertOutcomeAsync(AgentEffect effect, string outcome)
    {
        this.outcomesByKey[effect.IdempotencyKey] = outcome;

        return ValueTask.CompletedTask;
    }

    // Only an unfinished claim is given back. A key that already carries an outcome names an act
    // that happened, and forgetting that would turn run-once back into run-again.
    public ValueTask DeleteClaimAsync(AgentEffect effect)
    {
        this.outcomesByKey.TryRemove(
            new KeyValuePair<string, string?>(effect.IdempotencyKey, null));

        return ValueTask.CompletedTask;
    }
}
