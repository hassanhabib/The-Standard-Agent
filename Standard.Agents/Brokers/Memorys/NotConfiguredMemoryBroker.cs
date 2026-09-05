// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Memorys;

// No memory at all: nothing to recall, nowhere to store. What an agent composed without memory
// reads from, so the foundation keeps one shape rather than growing a nullable dependency.
public sealed class NotConfiguredMemoryBroker : IMemoryBroker
{
    public async ValueTask<IReadOnlyList<string>> SelectMemoriesAsync() =>
        [];

    public async ValueTask InsertMemoryAsync(string memory)
    {
    }
}
