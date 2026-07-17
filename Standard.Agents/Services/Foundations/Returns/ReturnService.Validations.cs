// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Returns.Exceptions;

namespace Standard.Agents.Services.Foundations.Returns;

public partial class ReturnService
{
    private static void ValidatePayload(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new InvalidReturnException(
                message: "Invalid return payload. Please correct the error and try again.");
        }
    }
        }
