// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace MinimalAgent;

public interface IAgent
{
    ValueTask<string> ProcessPromptAsync(string prompt);
}
