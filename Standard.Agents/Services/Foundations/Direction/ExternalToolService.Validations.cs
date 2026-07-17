// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.ExternalTools.Exceptions;

namespace Standard.Agents.Services.Foundations.Direction;

public partial class ExternalToolService
{
    // Only the name. An external tool may legitimately take no arguments, so
    // validating the input would reject valid work — same reasoning as
    // InternalToolService.
    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidExternalToolException(
                message: "Invalid external tool. Please correct the error and try again.");
        }
    }
}
