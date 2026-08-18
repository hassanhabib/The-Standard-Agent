// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Usages.Exceptions;

public class InvalidUsageException : Xeption
{
    public InvalidUsageException(string message)
        : base(message)
    { }
}
