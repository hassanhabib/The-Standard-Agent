// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Usages.Exceptions;

public class FailedUsageServiceException : Xeption
{
    public FailedUsageServiceException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
