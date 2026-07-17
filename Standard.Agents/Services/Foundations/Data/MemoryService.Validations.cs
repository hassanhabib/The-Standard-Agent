// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Memories.Exceptions;

namespace Standard.Agents.Services.Foundations.Data;

public partial class MemoryService
{
    // Only the write. An empty memory is not a memory — writing one appends a blank
    // line the store reads back forever as a memory that says nothing.
    private static void ValidateMemory(string memory)
    {
        if (string.IsNullOrWhiteSpace(memory))
        {
            throw new InvalidMemoryException(
                message: "Invalid memory. Please correct the error and try again.");
        }
    }
}
