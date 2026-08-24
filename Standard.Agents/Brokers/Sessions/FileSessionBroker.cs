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
    private readonly SemaphoreSlim writeLock = new(initialCount: 1, maxCount: 1);

    public FileSessionBroker(string sessionsPath) =>
        this.sessionsPath = Path.GetFullPath(sessionsPath);

    public async ValueTask<AgentSession?> SelectSessionAsync(string sessionId)
    {
        string sessionFile = FileFor(sessionId);

        if (File.Exists(sessionFile) is false)
        {
            return null;
        }

        string json = await File.ReadAllTextAsync(sessionFile);

        return JsonSerializer.Deserialize<AgentSession>(json, jsonOptions);
    }

    public async ValueTask UpsertSessionAsync(AgentSession session)
    {
        await this.writeLock.WaitAsync();

        try
        {
            Directory.CreateDirectory(this.sessionsPath);

            string sessionFile = FileFor(session.Id);
            string temporaryFile = $"{sessionFile}.writing";

            await File.WriteAllTextAsync(
                temporaryFile,
                JsonSerializer.Serialize(session, jsonOptions));

            File.Move(temporaryFile, sessionFile, overwrite: true);
        }
        finally
        {
            this.writeLock.Release();
        }
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
