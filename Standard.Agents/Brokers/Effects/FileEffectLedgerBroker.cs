// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using Standard.Agents.Models.Brokers.Effects;

namespace Standard.Agents.Brokers.Effects;

// The Local mode of the ledger: one JSON file per act, in a folder, so run-once survives the
// process (SPEC.md §4.9). Local and single-process by design - the atomic claim is the file
// system's create-new, which one machine honours; a fleet shares a ledger through UseEffectLedger.
public sealed class FileEffectLedgerBroker : IEffectLedgerBroker
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string ledgerPath;

    public FileEffectLedgerBroker(string ledgerPath) =>
        this.ledgerPath = Path.GetFullPath(ledgerPath);

    // Claim and write in one step: CreateNew refuses a key that already has a file, which is
    // exactly what run-once needs to know, and the record is written into the file so created.
    public async ValueTask<bool> InsertClaimAsync(EffectRecord claim)
    {
        Directory.CreateDirectory(this.ledgerPath);

        try
        {
            await using FileStream file = new(
                FileFor(claim.IdempotencyKey), FileMode.CreateNew, FileAccess.Write, FileShare.None);

            await JsonSerializer.SerializeAsync(file, claim, jsonOptions);

            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public async ValueTask<EffectRecord?> SelectRecordAsync(string idempotencyKey)
    {
        string file = FileFor(idempotencyKey);

        if (File.Exists(file) is false)
        {
            return null;
        }

        return JsonSerializer.Deserialize<EffectRecord>(await File.ReadAllTextAsync(file), jsonOptions);
    }

    // Written aside and moved into place, so a crash mid-write leaves the previous record
    // standing rather than a half-written one that would read as a different act.
    public async ValueTask UpdateRecordAsync(EffectRecord record)
    {
        Directory.CreateDirectory(this.ledgerPath);
        string file = FileFor(record.IdempotencyKey);
        string temporaryFile = $"{file}.writing";

        await File.WriteAllTextAsync(temporaryFile, JsonSerializer.Serialize(record, jsonOptions));
        File.Move(temporaryFile, file, overwrite: true);
    }

    public async ValueTask DeleteRecordAsync(string idempotencyKey) =>
        File.Delete(FileFor(idempotencyKey));

    // The key is already a hash (SPEC.md §4.9 requires it derived, never supplied), so it is a
    // safe file name as it stands.
    private string FileFor(string idempotencyKey) =>
        Path.Combine(this.ledgerPath, $"{idempotencyKey}.json");
}
