// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Memorys.Exceptions;

public class MemoryServiceException : Xeption
{
    public MemoryServiceException(string message, Xeption? innerException)
        : base(message, innerException)
    { }
}
