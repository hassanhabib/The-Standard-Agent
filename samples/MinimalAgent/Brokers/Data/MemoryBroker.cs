// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace MinimalAgent.Brokers.Data;

public sealed class MemoryBroker : IMemoryBroker
{
    public ValueTask<IReadOnlyList<string>> SelectMemoriesAsync() =>
        ValueTask.FromResult<IReadOnlyList<string>>([]);

    public ValueTask InsertMemoryAsync(string memory) =>
        ValueTask.CompletedTask;
}
