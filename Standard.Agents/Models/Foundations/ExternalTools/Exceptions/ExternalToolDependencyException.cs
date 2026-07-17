// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.ExternalTools.Exceptions;

public class ExternalToolDependencyException : Xeption
{
    public ExternalToolDependencyException(string message, Xeption? innerException)
        : base(message, innerException)
    { }
}
