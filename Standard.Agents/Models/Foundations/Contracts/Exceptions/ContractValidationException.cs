// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Contracts.Exceptions;

public class ContractValidationException : Xeption
{
    public ContractValidationException(string message, Xeption? innerException)
        : base(message, innerException)
    { }
}
