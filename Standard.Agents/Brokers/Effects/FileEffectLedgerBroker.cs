// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
using Standard.Agents.Models.Orchestrations.Effects;

namespace Standard.Agents.Brokers.Effects;

// Run-once that survives the process. One file per idempotency key, in a folder.
//
// The in-memory ledger covers a retry inside a run and a repeat proposal across turns. It cannot
// cover the case an auditor actually asks about: the run was killed immediately after the wire
// transfer went out, and something resumed it. There the claim has to outlive the process that
// made it, so it lives on disk.
//
// The claim is the file's creation, not its content. CreateNew fails if the file exists, and the
// filesystem decides that atomically — so two processes proposing the same act cannot both
// conclude they are the first. The outcome is written into the claimed file afterwards; a claim
// with no outcome yet means the effect is in flight, which is deliberately not the same as
// "never happened" (SPEC.md §4.9).
public sealed class FileEffectLedgerBroker : IEffectLedgerBroker
{
    private const string InFlight = "effect already in progress";

    private readonly string ledgerPath;

    public FileEffectLedgerBroker(string ledgerPath) =>
        this.ledgerPath = Path.GetFullPath(ledgerPath);

    public async ValueTask<string?> SelectOutcomeAsync(AgentEffect effect)
    {
        Directory.CreateDirectory(this.ledgerPath);

        string claimFile = FileFor(effect);

        try
        {
            // Claim and report in one step. The exception is the answer, not a failure: it says
            // another run got here first, which is exactly what run-once needs to know.
            using FileStream claim = new(
                claimFile, FileMode.CreateNew, FileAccess.Write, FileShare.None);

            return null;
        }
        catch (IOException)
        {
            string recorded = await ReadOutcomeAsync(claimFile);

            return string.IsNullOrEmpty(recorded) ? InFlight : recorded;
        }
    }

    public async ValueTask InsertOutcomeAsync(AgentEffect effect, string outcome)
    {
        Directory.CreateDirectory(this.ledgerPath);

        string claimFile = FileFor(effect);
        string temporaryFile = $"{claimFile}.writing";

        var record = new LedgerRecord(
            effect.IdempotencyKey, effect.ToolName, outcome);

        await File.WriteAllTextAsync(temporaryFile, JsonSerializer.Serialize(record));

        // Written aside and moved into place, so a crash mid-write leaves the claim standing
        // rather than a half-written outcome that would read as a different act.
        File.Move(temporaryFile, claimFile, overwrite: true);
    }

    // Only an unfinished claim is given back. A file that already carries an outcome names an act
    // that happened, and deleting it would turn run-once back into run-again.
    public async ValueTask DeleteClaimAsync(AgentEffect effect)
    {
        string claimFile = FileFor(effect);

        if (File.Exists(claimFile) is false)
        {
            return;
        }

        if (string.IsNullOrEmpty(await ReadOutcomeAsync(claimFile)) is false)
        {
            return;
        }

        try
        {
            File.Delete(claimFile);
        }
        catch (IOException)
        {
            // Another process is holding it; it owns the claim, and forcing it open would be
            // worse than leaving a claim that a later run will read as in flight.
        }
    }

    // A ledger written by a process that then died can be read by one that is still starting up,
    // so the read tolerates a claim that is empty or being replaced rather than faulting.
    private static async ValueTask<string> ReadOutcomeAsync(string claimFile)
    {
        try
        {
            string json = await File.ReadAllTextAsync(claimFile);

            return string.IsNullOrWhiteSpace(json)
                ? string.Empty
                : JsonSerializer.Deserialize<LedgerRecord>(json)?.Outcome ?? string.Empty;
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            return string.Empty;
        }
    }

    // The key is already a hash (SPEC.md §4.9 requires it derived, never supplied), so it is a
    // safe file name as it stands.
    private string FileFor(AgentEffect effect) =>
        Path.Combine(this.ledgerPath, $"{effect.IdempotencyKey}.json");

    private sealed record LedgerRecord(string Key, string ToolName, string Outcome);
}
