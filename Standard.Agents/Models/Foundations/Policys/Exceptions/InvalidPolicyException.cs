// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Policys.Exceptions;

public class InvalidPolicyException : Xeption
{
    public InvalidPolicyException(string message)
        : base(message)
    { }
}
