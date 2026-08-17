// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Approvals.Exceptions;

public class FailedApprovalServiceException : Xeption
{
    public FailedApprovalServiceException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
