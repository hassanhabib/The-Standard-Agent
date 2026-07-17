// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Brains.Exceptions;

public class FailedBrainDependencyException : Xeption
{
    public FailedBrainDependencyException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
