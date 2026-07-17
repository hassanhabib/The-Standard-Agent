// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using MinimalAgent.Brokers.Data;

namespace MinimalAgent.Services.Foundations.Data;

public sealed class KnowledgeService : IKnowledgeService
{
    private readonly IKnowledgeBroker knowledgeBroker;

    public KnowledgeService(IKnowledgeBroker knowledgeBroker) =>
        this.knowledgeBroker = knowledgeBroker;

    public async ValueTask<IReadOnlyList<string>> RetrieveKnowledgeAsync(string query) =>
        await this.knowledgeBroker.SelectKnowledgeAsync(query);
}
