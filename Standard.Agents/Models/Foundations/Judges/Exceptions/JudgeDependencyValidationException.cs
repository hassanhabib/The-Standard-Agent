// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Judges.Exceptions;

public class JudgeDependencyValidationException : Xeption
{
    public JudgeDependencyValidationException(string message, Xeption innerException)
        : base(message, innerException)
    { }
}
