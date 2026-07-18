// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Memorys.Exceptions;

namespace Standard.Agents.Services.Foundations.Memorys;

public partial class MemoryService
{
    private static void ValidateMemory(string memory)
    {
        if (string.IsNullOrWhiteSpace(memory))
        {
            throw new InvalidMemoryException(
                message: "Invalid memory. Please correct the error and try again.");
        }
    }
}
