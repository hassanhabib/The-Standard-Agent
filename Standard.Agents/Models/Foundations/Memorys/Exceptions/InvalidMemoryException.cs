// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Memorys.Exceptions;

public class InvalidMemoryException : Xeption
{
    public InvalidMemoryException(string message)
        : base(message)
    { }
}
