// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Clients.Agents.Exceptions;

/// <summary>
/// A configuration document the agent cannot honestly compose from — malformed JSON, a key it
/// does not know, or a value of the wrong shape. Always loud and always named: a typo'd key
/// silently ignored is a control the caller believes is on and is not.
/// </summary>
public class InvalidAgentConfigurationException : Xeption
{
    public InvalidAgentConfigurationException(string message)
        : base(message)
    { }
}
