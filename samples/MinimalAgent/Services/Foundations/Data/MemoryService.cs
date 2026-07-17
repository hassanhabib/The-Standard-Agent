// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using MinimalAgent.Brokers.Data;

namespace MinimalAgent.Services.Foundations.Data;

public sealed class MemoryService : IMemoryService
{
    private readonly IMemoryBroker memoryBroker;

    public MemoryService(IMemoryBroker memoryBroker) =>
        this.memoryBroker = memoryBroker;

    public async ValueTask<IReadOnlyList<string>> RecallMemoriesAsync() =>
        await this.memoryBroker.SelectMemoriesAsync();

    public async ValueTask RememberAsync(string memory) =>
        await this.memoryBroker.InsertMemoryAsync(memory);
}
