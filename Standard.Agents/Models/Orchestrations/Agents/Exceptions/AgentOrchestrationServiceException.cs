// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Orchestrations.Agents.Exceptions;

public class AgentOrchestrationServiceException : Xeption
{
    public AgentOrchestrationServiceException(string message, Xeption? innerException)
        : base(message, innerException)
    { }
}
