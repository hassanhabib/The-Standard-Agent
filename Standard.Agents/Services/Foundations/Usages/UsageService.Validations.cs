// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Usages.Exceptions;

namespace Standard.Agents.Services.Foundations.Usages;

public partial class UsageService
{
    // Empty text is a legitimate measurement — a call with no completion consumed no completion
    // tokens. Null is not: it is a caller that lost the text it meant to charge for, and
    // measuring it as zero would quietly under-bill the run.
    private static void ValidateText(string text, string name)
    {
        if (text is null)
        {
            throw new InvalidUsageException(
                message: $"Invalid usage, {name} is required. "
                    + "Please correct the error and try again.");
        }
    }

    // A negative count would subtract from the run's spend, which turns a budget into a thing an
    // unusual reply can extend. A custom counter is host code, so this is reachable.
    private static void ValidateCount(int tokens)
    {
        if (tokens < 0)
        {
            throw new InvalidUsageException(
                message: "Invalid usage, token count cannot be negative. "
                    + "Please correct the error and try again.");
        }
    }
}
