// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace MinimalAgent.Brokers.Data;

public interface IMemoryBroker
{
    ValueTask<IReadOnlyList<string>> SelectMemoriesAsync();

    ValueTask InsertMemoryAsync(string memory);
}
