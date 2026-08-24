// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Memorys;

namespace Standard.Agents.Conformance;

public sealed class StubMemoryBroker : IMemoryBroker
{
    private readonly IReadOnlyList<string> memories;

    // Empty by default; a vector may seed it — which is how a POISONED memory is certified:
    // the injected instruction rides the same seam a real remembered preference would.
    public StubMemoryBroker(IReadOnlyList<string>? memories = null) =>
        this.memories = memories ?? [];

    public async ValueTask<IReadOnlyList<string>> SelectMemoriesAsync() =>
        this.memories;

    public ValueTask InsertMemoryAsync(string memory) =>
        ValueTask.CompletedTask;
}
