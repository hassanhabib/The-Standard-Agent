// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Approvals.Exceptions;

public class InvalidApprovalException : Xeption
{
    public InvalidApprovalException(string message)
        : base(message)
    { }
}
