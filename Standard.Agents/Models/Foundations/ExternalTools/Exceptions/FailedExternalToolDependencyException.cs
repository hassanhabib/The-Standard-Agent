// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.ExternalTools.Exceptions;

public class FailedExternalToolDependencyException : Xeption
{
    public FailedExternalToolDependencyException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
