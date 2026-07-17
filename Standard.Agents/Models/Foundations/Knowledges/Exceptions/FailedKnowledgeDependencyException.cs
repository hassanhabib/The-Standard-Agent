// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Knowledges.Exceptions;

public class FailedKnowledgeDependencyException : Xeption
{
    public FailedKnowledgeDependencyException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
