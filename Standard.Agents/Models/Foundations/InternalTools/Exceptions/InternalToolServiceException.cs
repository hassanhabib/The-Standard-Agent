// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.InternalTools.Exceptions;

public class InternalToolServiceException : Xeption
{
    public InternalToolServiceException(string message, Xeption? innerException)
        : base(message, innerException)
    { }
}
