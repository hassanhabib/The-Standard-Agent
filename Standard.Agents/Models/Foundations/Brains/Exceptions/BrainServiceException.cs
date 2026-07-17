// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Brains.Exceptions;

public class BrainServiceException : Xeption
{
    public BrainServiceException(string message, Xeption? innerException)
        : base(message, innerException)
    { }
}
