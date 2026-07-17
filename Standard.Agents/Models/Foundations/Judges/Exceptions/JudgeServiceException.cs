// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Judges.Exceptions;

public class JudgeServiceException : Xeption
{
    public JudgeServiceException(string message, Xeption? innerException)
        : base(message, innerException)
    { }
}
