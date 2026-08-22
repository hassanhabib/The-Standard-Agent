// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Contracts.Exceptions;

public class FailedContractDependencyException : Xeption
{
    public FailedContractDependencyException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
