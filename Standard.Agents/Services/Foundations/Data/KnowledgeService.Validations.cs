// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Knowledges.Exceptions;

namespace Standard.Agents.Services.Foundations.Data;

public partial class KnowledgeService
{
    // An empty query is not a search. The default broker matches by substring and
    // every document contains the empty string, so an unvalidated empty query would
    // return the whole corpus and flood the context window.
    private static void ValidateQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new InvalidKnowledgeException(
                message: "Invalid knowledge query. Please correct the error and try again.");
        }
    }
}
