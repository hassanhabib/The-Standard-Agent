// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Coordinations.Agents.Exceptions;

namespace Standard.Agents.Services.Coordinations;

public partial class AgentCoordinationService
{
    private static void ValidatePrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new InvalidAgentException(
                message: "Invalid prompt. Please correct the error and try again.");
        }
    }
}
