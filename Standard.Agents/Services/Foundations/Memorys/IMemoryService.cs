// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Services.Foundations.Memorys;

public interface IMemoryService
{
    ValueTask<IReadOnlyList<string>> RecallMemoriesAsync();

    ValueTask RememberAsync(string memory);
}
