// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Returns.Exceptions;

public class InvalidReturnException : Xeption
{
    public InvalidReturnException(string message)
        : base(message)
    { }
}
