// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Sessions.Exceptions;

/// <summary>
/// A session write based on a read that is no longer current: another prompt in the same session
/// wrote first. The store refused it rather than letting the last writer erase a completed turn;
/// the caller re-reads and tries again.
/// </summary>
public class StaleSessionException : Xeption
{
    public StaleSessionException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
