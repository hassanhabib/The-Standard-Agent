// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Data;
using Standard.Agents.Brokers.Loggings;

namespace Standard.Agents.Services.Foundations.Data;

public partial class MemoryService : IMemoryService
{
    private readonly IMemoryBroker memoryBroker;
    private readonly ILoggingBroker loggingBroker;

    public MemoryService(
        IMemoryBroker memoryBroker,
        ILoggingBroker loggingBroker)
    {
        this.memoryBroker = memoryBroker;
        this.loggingBroker = loggingBroker;
    }

    public async ValueTask<IReadOnlyList<string>> RecallMemoriesAsync() =>
        await this.memoryBroker.SelectMemoriesAsync();

    public async ValueTask RememberAsync(string memory) =>
        await this.memoryBroker.InsertMemoryAsync(memory);
}
