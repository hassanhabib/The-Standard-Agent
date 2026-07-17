// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace MinimalAgent.Services.Foundations.Data;

public interface IKnowledgeService
{
    ValueTask<IReadOnlyList<string>> RetrieveKnowledgeAsync(string query);
}
