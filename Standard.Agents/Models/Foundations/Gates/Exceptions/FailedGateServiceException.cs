// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Gates.Exceptions;

public class FailedGateServiceException : Xeption
{
    public FailedGateServiceException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
