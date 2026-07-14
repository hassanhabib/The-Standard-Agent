// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents;

public interface IAgent
{
    ValueTask<string> ProcessPromptAsync(string prompt);
}
