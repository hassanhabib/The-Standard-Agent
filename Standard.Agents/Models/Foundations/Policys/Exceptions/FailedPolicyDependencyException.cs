// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Policys.Exceptions;

public class FailedPolicyDependencyException : Xeption
{
    public FailedPolicyDependencyException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
