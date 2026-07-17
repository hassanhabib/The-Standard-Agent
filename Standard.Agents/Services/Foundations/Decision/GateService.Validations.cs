// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Gates.Exceptions;

namespace Standard.Agents.Services.Foundations.Decision;

public partial class GateService
{
                private static void ValidateScreen(string gatePrompt, string input)
    {
        if (string.IsNullOrWhiteSpace(gatePrompt) || string.IsNullOrWhiteSpace(input))
        {
            throw new InvalidGateException(
                message: "Invalid gate input. Please correct the error and try again.");
        }
    }
}
