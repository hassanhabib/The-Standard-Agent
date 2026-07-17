// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Brains.Exceptions;

public class FailedBrainServiceException : Xeption
{
    public FailedBrainServiceException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
