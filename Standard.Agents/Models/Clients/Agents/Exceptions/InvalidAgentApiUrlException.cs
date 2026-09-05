// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Clients.Agents.Exceptions;

/// <summary>
/// An endpoint the agent cannot honestly call. The API URL is the base a route is appended to,
/// so its shape is load-bearing: without a trailing slash the route resolves against the parent,
/// and a base that already names the route reaches it twice. Either fails at the first prompt
/// with a 404 that blames the provider, so the contradiction is refused at composition instead.
/// </summary>
public class InvalidAgentApiUrlException : Xeption
{
    public InvalidAgentApiUrlException(string message)
        : base(message)
    { }
}
