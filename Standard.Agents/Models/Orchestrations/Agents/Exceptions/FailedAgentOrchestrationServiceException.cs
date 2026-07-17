// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Orchestrations.Agents.Exceptions;

public class FailedAgentOrchestrationServiceException : Xeption
{
    public FailedAgentOrchestrationServiceException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
