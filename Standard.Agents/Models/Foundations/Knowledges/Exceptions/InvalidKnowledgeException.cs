// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Knowledges.Exceptions;

public class InvalidKnowledgeException : Xeption
{
    public InvalidKnowledgeException(string message)
        : base(message)
    { }
}
