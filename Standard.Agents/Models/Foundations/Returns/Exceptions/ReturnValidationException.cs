// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Returns.Exceptions;

public class ReturnValidationException : Xeption
{
    public ReturnValidationException(string message, Xeption innerException)
        : base(message, innerException)
    { }
}
