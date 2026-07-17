// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Judges.Exceptions;

public class FailedJudgeServiceException : Xeption
{
    public FailedJudgeServiceException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
