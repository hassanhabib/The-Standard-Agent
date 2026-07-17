// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Knowledges.Exceptions;

namespace Standard.Agents.Services.Foundations.Knowledges;

public partial class KnowledgeService
{
                private static void ValidateQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new InvalidKnowledgeException(
                message: "Invalid knowledge query. Please correct the error and try again.");
        }
    }
}
