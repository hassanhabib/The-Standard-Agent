// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Gates.Exceptions;

public class GateDependencyException : Xeption
{
    public GateDependencyException(string message, Xeption innerException)
        : base(message, innerException)
    { }
}
