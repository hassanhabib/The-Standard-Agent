// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Services.Foundations.Direction;

public interface IInternalToolService
{
    bool Handles(string toolName);

    ValueTask<string> RunAsync(string toolName, string input);
}
