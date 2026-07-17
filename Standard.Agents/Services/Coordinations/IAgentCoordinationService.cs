// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Services.Coordinations;

public interface IAgentCoordinationService
{
    ValueTask<string> ProcessPromptAsync(string prompt);
}
