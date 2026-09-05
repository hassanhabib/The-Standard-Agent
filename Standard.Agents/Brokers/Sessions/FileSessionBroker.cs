// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using Standard.Agents.Models.Brokers.Sessions;

namespace Standard.Agents.Brokers.Sessions;

// The Local mode: one JSON file per session in a folder.
//
// Written to a temporary file and moved into place, so a crash mid-write leaves the previous
// session intact rather than a half-written one. A session is the thing resumption depends on;
// corrupting it while saving it would defeat the feature at exactly the moment it is needed.
//
// It is a real store rather than an in-memory one on purpose: SPEC.md §4.11 requires resumption
// to work from a DIFFERENT process, and an in-memory session cannot demonstrate that.
public sealed class FileSessionBroker : ISessionBroker
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        WriteIndented = true
    };

    private readonly string sessionsPath;

    // One lock over reads and writes alike: a move over a file another prompt is reading fails
    // on Windows with access denied, which is how two prompts in one session crashed a run
    // (principal review 2026-09-04, F-06). Per instance — a second process gets its own lock,
    // which is the limit of a file and the reason a store shared across replicas is a session
    // broker of its own.
    private readonly SemaphoreSlim fileLock = new(initialCount: 1, maxCount: 1);

    public FileSessionBroker(string sessionsPath) =>
        this.sessionsPath = Path.GetFullPath(sessionsPath);

    public async ValueTask<AgentSession?> SelectSessionAsync(string sessionId)
    {
        await this.fileLock.WaitAsync();

        try
        {
            return await ReadSessionAsync(FileFor(sessionId));
        }
        finally
        {
            this.fileLock.Release();
        }
    }

    // Compare-and-swap on the version, a file's stand-in for a store's own: a write based on a
    // read that is no longer current is refused with the concurrency failure the foundation
    // localizes, rather than erasing the turn a faster writer stored.
    public async ValueTask UpsertSessionAsync(AgentSession session)
    {
        await this.fileLock.WaitAsync();

        try
        {
            Directory.CreateDirectory(this.sessionsPath);

            string sessionFile = FileFor(session.Id);
            long storedVersion = (await ReadSessionAsync(sessionFile))?.Version ?? 0;

            if (session.Version != storedVersion + 1)
            {
                throw new System.Data.DBConcurrencyException(
                    $"Session '{session.Id}' is at version {storedVersion}; this write was "
                        + $"based on version {session.Version - 1}.");
            }

            string temporaryFile = $"{sessionFile}.writing";

            await File.WriteAllTextAsync(
                temporaryFile,
                JsonSerializer.Serialize(session, jsonOptions));

            File.Move(temporaryFile, sessionFile, overwrite: true);
        }
        finally
        {
            this.fileLock.Release();
        }
    }

    private static async ValueTask<AgentSession?> ReadSessionAsync(string sessionFile)
    {
        if (File.Exists(sessionFile) is false)
        {
            return null;
        }

        string json = await File.ReadAllTextAsync(sessionFile);

        return JsonSerializer.Deserialize<AgentSession>(json, jsonOptions);
    }

    // A session id comes from the host and may be anything — an email, a ticket reference, a
    // path fragment. Hashing it keeps it off the filesystem's grammar entirely rather than
    // trying to sanitize a name that could escape the folder.
    private string FileFor(string sessionId)
    {
        // Convert.ToHexStringLower is .NET 9+. Both forms emit identical lowercase hex, which
        // is load-bearing: a session written on one target must be found on the other.
#if NET9_0_OR_GREATER
        string safeName = Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(sessionId)))[..32];
#else
        string safeName = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(sessionId))).ToLowerInvariant()[..32];
#endif

        return Path.Combine(this.sessionsPath, $"{safeName}.json");
    }
}
