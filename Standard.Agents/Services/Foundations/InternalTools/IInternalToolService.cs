// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Services.Foundations.InternalTools;

public interface IInternalToolService
{
    ValueTask<bool> HandlesAsync(string name);

    ValueTask<string> RunAsync(string name, string input);

    /// <summary>
    /// Undoes what a tool did, where the tool knows how (SPEC.md §4.9). Returns what was done, or
    /// an empty string when the tool declared no way back.
    /// </summary>
    ValueTask<string> CompensateAsync(string name, string input, string outcome);
}
