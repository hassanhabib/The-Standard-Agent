// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Memorys.Exceptions;

public class FailedMemoryDependencyException : Xeption
{
    public FailedMemoryDependencyException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
