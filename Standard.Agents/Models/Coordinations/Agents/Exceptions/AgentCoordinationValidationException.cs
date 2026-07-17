// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Coordinations.Agents.Exceptions;

public class AgentCoordinationValidationException : Xeption
{
    public AgentCoordinationValidationException(string message, Xeption innerException)
        : base(message, innerException)
    { }
}
