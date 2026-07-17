// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Clients.Agents.Exceptions;

public class InvalidAgentCompositionException : Xeption
{
    public InvalidAgentCompositionException(string message)
        : base(message)
    { }
}
