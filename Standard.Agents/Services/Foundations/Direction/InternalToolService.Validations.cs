// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.InternalTools.Exceptions;

namespace Standard.Agents.Services.Foundations.Direction;

public partial class InternalToolService
{
    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidInternalToolException(
                message: "Invalid internal tool. Please correct the error and try again.");
        }
    }
}
