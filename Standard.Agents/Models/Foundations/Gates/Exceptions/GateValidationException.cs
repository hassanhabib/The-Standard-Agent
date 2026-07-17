// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Gates.Exceptions;

public class GateValidationException : Xeption
{
    public GateValidationException(string message, Xeption innerException)
        : base(message, innerException)
    { }
}
