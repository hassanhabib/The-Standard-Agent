// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Memorys;

namespace Standard.Agents.Conformance;

public sealed class StubMemoryBroker : IMemoryBroker
{
    public ValueTask<IReadOnlyList<string>> SelectMemoriesAsync() =>
        ValueTask.FromResult<IReadOnlyList<string>>([]);

    public ValueTask InsertMemoryAsync(string memory) =>
        ValueTask.CompletedTask;
    }
