// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Knowledges.Exceptions;

public class KnowledgeValidationException : Xeption
{
    public KnowledgeValidationException(string message, Xeption innerException)
        : base(message, innerException)
    { }
}
