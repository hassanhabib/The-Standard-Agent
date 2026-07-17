// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Brains.Exceptions;

public class InvalidBrainException : Xeption
{
    public InvalidBrainException(string message)
        : base(message)
    { }
}
