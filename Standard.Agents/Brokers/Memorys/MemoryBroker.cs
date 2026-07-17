// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Memorys;

public sealed class MemoryBroker : IMemoryBroker
{
    private readonly string memoryPath;

    public MemoryBroker(string memoryPath)
    {
        this.memoryPath = Path.GetFullPath(memoryPath);

        EnsureStoreExists(this.memoryPath);
    }

    public async ValueTask<IReadOnlyList<string>> SelectMemoriesAsync() =>
        await File.ReadAllLinesAsync(this.memoryPath);

    public async ValueTask InsertMemoryAsync(string memory) =>
await File.AppendAllLinesAsync(this.memoryPath, [memory]);

    private static void EnsureStoreExists(string memoryPath)
    {
        string directoryPath = Path.GetDirectoryName(memoryPath)!;

        Directory.CreateDirectory(directoryPath);

        File.AppendAllText(memoryPath, string.Empty);
    }
}
