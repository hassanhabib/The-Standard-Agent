// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Judges.Exceptions;

public class InvalidJudgeException : Xeption
{
    public InvalidJudgeException(string message)
        : base(message)
    { }
}
