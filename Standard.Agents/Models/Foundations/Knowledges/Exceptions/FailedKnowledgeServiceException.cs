// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Knowledges.Exceptions;

public class FailedKnowledgeServiceException : Xeption
{
    public FailedKnowledgeServiceException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
