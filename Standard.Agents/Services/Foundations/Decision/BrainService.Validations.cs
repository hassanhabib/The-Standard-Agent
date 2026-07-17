// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Brains.Exceptions;

namespace Standard.Agents.Services.Foundations.Decision;

public partial class BrainService
{
            private static void ValidateUserPrompt(string userPrompt)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            throw new InvalidBrainException(
                message: "Invalid brain input. Please correct the error and try again.");
        }
    }
}
