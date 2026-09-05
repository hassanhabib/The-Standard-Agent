// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Sessions.Exceptions;

public class SessionDependencyValidationException : Xeption
{
    public SessionDependencyValidationException(string message, Xeption innerException)
        : base(message, innerException)
    { }
}
