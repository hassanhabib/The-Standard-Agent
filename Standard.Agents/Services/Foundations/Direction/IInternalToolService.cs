// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Services.Foundations.Direction;

public interface IInternalToolService
{
    ValueTask<bool> HandlesAsync(string name);

    ValueTask<string> RunAsync(string name, string input);
}
