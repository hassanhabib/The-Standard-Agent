// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Memorys;

namespace Standard.Agents.Services.Foundations.Memorys;

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

    public ValueTask<IReadOnlyList<string>> RecallMemoriesAsync() =>
    TryCatch(async () =>
        await this.memoryBroker.SelectMemoriesAsync());

    public ValueTask RememberAsync(string memory) =>
    TryCatch(async () =>
    {
        ValidateMemory(memory);

        await this.memoryBroker.InsertMemoryAsync(memory);
    });
    }
