// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Memorys;

namespace Standard.Agents.Conformance;

public sealed class StubMemoryBroker : IMemoryBroker
{
    public async ValueTask<IReadOnlyList<string>> SelectMemoriesAsync() =>
        [];

    public ValueTask InsertMemoryAsync(string memory) =>
        ValueTask.CompletedTask;
}
