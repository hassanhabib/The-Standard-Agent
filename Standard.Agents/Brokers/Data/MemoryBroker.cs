// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Data;

// A durable store that outlives the agent instance. Theory Ch.7: the agent is
// ephemeral compute; memory is a durable, external resource. Swap this for a
// database or a vector store via IMemoryBroker — the file is the default, not the
// design.
//
// One memory per line, so a read is a read and an append is an append. A JSON
// document would need a full parse-and-rewrite to add one entry, turning every
// write into a chance to lose the file.
public sealed class MemoryBroker : IMemoryBroker
{
    private readonly string memoryPath;

    public MemoryBroker(string memoryPath)
    {
        this.memoryPath = Path.GetFullPath(memoryPath);

        // The broker owns its resource, which includes bringing it into existence —
        // the same reason StorageBroker calls Database.Migrate() in its constructor.
        // Doing it here keeps the read path free of an existence check, which would
        // be flow control a broker is not allowed to have.
        EnsureStoreExists(this.memoryPath);
    }

    public async ValueTask<IReadOnlyList<string>> SelectMemoriesAsync() =>
        await File.ReadAllLinesAsync(this.memoryPath);

    // Append, never rewrite — the store only grows. Forgetting is a business
    // decision and would arrive as its own routine, not as a side effect of
    // remembering.
    public async ValueTask InsertMemoryAsync(string memory) =>
        await File.AppendAllLinesAsync(this.memoryPath, [memory]);

    private static void EnsureStoreExists(string memoryPath)
    {
        string directoryPath = Path.GetDirectoryName(memoryPath)!;

        Directory.CreateDirectory(directoryPath);

        // Create-if-absent without truncating an existing store. `new FileStream`
        // with OpenOrCreate would leave the file open; File.AppendAllText of nothing
        // is the shortest honest way to say "make sure this exists, change nothing".
        File.AppendAllText(memoryPath, string.Empty);
    }
}
