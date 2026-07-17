// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Memorys;

public interface IMemoryBroker
{
    ValueTask<IReadOnlyList<string>> SelectMemoriesAsync();

    ValueTask InsertMemoryAsync(string memory);
}
