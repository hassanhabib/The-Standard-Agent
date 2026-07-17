// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Data;

public interface IMemoryBroker
{
    ValueTask<IReadOnlyList<string>> SelectMemoriesAsync();
}
