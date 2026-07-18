// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Memorys;

public sealed class MemoryBroker : IMemoryBroker
{
    private readonly string memoryPath;

    public MemoryBroker(string memoryPath) =>
        this.memoryPath = Path.GetFullPath(memoryPath);

    public async ValueTask<IReadOnlyList<string>> SelectMemoriesAsync() =>
        File.Exists(this.memoryPath)
            ? await File.ReadAllLinesAsync(this.memoryPath)
            : [];

    public async ValueTask InsertMemoryAsync(string memory)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(this.memoryPath)!);

        await File.AppendAllLinesAsync(this.memoryPath, [memory]);
    }
}
