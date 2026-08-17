// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Memorys;

// The Custom mode's substrate for memory (SPEC.md §4.8).
public sealed class FunctionMemoryBroker : IMemoryBroker
{
    private readonly Func<ValueTask<IReadOnlyList<string>>> recall;
    private readonly Func<string, ValueTask> remember;

    public FunctionMemoryBroker(
        Func<ValueTask<IReadOnlyList<string>>> recall,
        Func<string, ValueTask> remember)
    {
        this.recall = recall;
        this.remember = remember;
    }

    public ValueTask<IReadOnlyList<string>> SelectMemoriesAsync() =>
        this.recall();

    public ValueTask InsertMemoryAsync(string memory) =>
        this.remember(memory);
}
