// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Coordinations.Agents.Exceptions;

public class FailedRunManagementServiceException : Xeption
{
    public FailedRunManagementServiceException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
