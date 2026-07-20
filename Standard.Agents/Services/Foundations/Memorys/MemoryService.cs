// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Files;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Memorys;

namespace Standard.Agents.Services.Foundations.Memorys;

public partial class MemoryService : IMemoryService
{
    private readonly IMemoryBroker? memoryBroker;
    private readonly IFileBroker? fileBroker;
    private readonly string memoryPath;
    private readonly ILoggingBroker loggingBroker;

    public MemoryService(
        IMemoryBroker memoryBroker,
        ILoggingBroker loggingBroker)
    {
        this.memoryBroker = memoryBroker;
        this.memoryPath = string.Empty;
        this.loggingBroker = loggingBroker;
    }

    public MemoryService(
        IFileBroker fileBroker,
        string memoryPath,
        ILoggingBroker loggingBroker)
    {
        this.fileBroker = fileBroker;
        this.memoryPath = memoryPath;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<IReadOnlyList<string>> RecallMemoriesAsync() =>
    TryCatch(async () =>
    {
        return this.fileBroker is not null
            ? await SelectMemoriesFromFileAsync(this.fileBroker)
            : await this.memoryBroker!.SelectMemoriesAsync();
    });

    public ValueTask RememberAsync(string memory) =>
    TryCatch(async () =>
    {
        ValidateMemory(memory);

        if (this.fileBroker is not null)
        {
            await InsertMemoryIntoFileAsync(this.fileBroker, memory);
        }
        else
        {
            await this.memoryBroker!.InsertMemoryAsync(memory);
        }
    });

    private async ValueTask<IReadOnlyList<string>> SelectMemoriesFromFileAsync(IFileBroker fileBroker)
    {
        if (fileBroker.FileExists(this.memoryPath) is false)
        {
            return [];
        }

        return await fileBroker.ReadAllLinesAsync(this.memoryPath);
    }

    private async ValueTask InsertMemoryIntoFileAsync(IFileBroker fileBroker, string memory)
    {
        fileBroker.CreateDirectory(Path.GetDirectoryName(this.memoryPath)!);

        await fileBroker.AppendAllLinesAsync(this.memoryPath, [memory]);
    }
}
