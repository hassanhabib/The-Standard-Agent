// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Knowledges.Exceptions;

public class KnowledgeServiceException : Xeption
{
    public KnowledgeServiceException(string message, Xeption innerException)
        : base(message, innerException)
    { }
}
