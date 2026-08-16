// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Standard.Agents.Models.Brokers.Audits;

namespace Standard.Agents.Brokers.Audits;

// The decision log's file sink — one JSON object per line (SPEC.md §4.7).
//
// The broker owns three sink mechanics and no business flow: it appends, it serializes
// concurrent writers, and it chains hashes. The chain lives here because only the sink
// knows what the previous record on THIS sink was.
//
// Each write appends and closes rather than holding the file open. A decision log is read
// while it is being written — by a tail, a shipper, a test — and a retained write handle
// locks those readers out on Windows. The cost of reopening is paid once per record and
// buys a sink anything can read at any moment.
public sealed class FileAuditBroker : IAuditBroker
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string auditPath;
    private readonly SemaphoreSlim writeLock = new(initialCount: 1, maxCount: 1);

    private bool chainSeeded;
    private string? previousHash;

    public FileAuditBroker(string auditPath) =>
        this.auditPath = Path.GetFullPath(auditPath);

    public async ValueTask WriteAsync(AuditRecord record)
    {
        // One writer at a time. Records from concurrent runs may interleave, but a record is
        // never interleaved into — SPEC.md §4.7.
        await this.writeLock.WaitAsync();

        try
        {
            SeedChain();

            AuditRecord chainedRecord = record with { PreviousHash = this.previousHash };
            string line = JsonSerializer.Serialize(chainedRecord, jsonOptions);

            // Append, never truncate. This is the durability rule (SPEC.md §4.7); there is
            // deliberately no code path in this broker that can shorten the sink.
            await File.AppendAllTextAsync(this.auditPath, line + Environment.NewLine);

            this.previousHash = Hash(line);
        }
        finally
        {
            this.writeLock.Release();
        }
    }

    // Verifies a sink written by this broker: every record's previousHash must equal the hash
    // of the line before it. Returns the 1-based line number of the first record that does
    // not, or null when the chain is intact — so a caller can prove a decision log was neither
    // altered nor truncated (SPEC.md §4.7).
    public static int? FindBrokenLink(IEnumerable<string> lines)
    {
        string? expectedHash = null;
        int lineNumber = 0;

        foreach (string line in lines)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            AuditRecord? record = JsonSerializer.Deserialize<AuditRecord>(line, jsonOptions);

            if (record?.PreviousHash != expectedHash)
            {
                return lineNumber;
            }

            expectedHash = Hash(line);
        }

        return null;
    }

    // Continues an existing sink's chain rather than restarting it, so a process restart does
    // not read as tampering. Done once, on the first write, because the sink may not exist yet
    // when the broker is constructed.
    private void SeedChain()
    {
        if (this.chainSeeded)
        {
            return;
        }

        this.chainSeeded = true;

        string? directory = Path.GetDirectoryName(this.auditPath);

        if (string.IsNullOrEmpty(directory) is false)
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(this.auditPath) is false)
        {
            return;
        }

        string? lastLine = File.ReadLines(this.auditPath)
            .LastOrDefault(line => string.IsNullOrWhiteSpace(line) is false);

        this.previousHash = lastLine is null ? null : Hash(lastLine);
    }

    private static string Hash(string line) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(line)));
}
