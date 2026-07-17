// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.ExternalTools.Exceptions;

namespace Standard.Agents.Services.Foundations.Direction;

public partial class ExternalToolService
{
                private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidExternalToolException(
                message: "Invalid external tool. Please correct the error and try again.");
        }
    }
}
