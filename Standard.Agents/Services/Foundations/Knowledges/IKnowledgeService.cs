// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Services.Foundations.Knowledges;

public interface IKnowledgeService
{
    ValueTask<IReadOnlyList<string>> RetrieveKnowledgeAsync(string query);
}
