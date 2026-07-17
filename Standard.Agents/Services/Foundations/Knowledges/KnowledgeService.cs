// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Knowledges;
using Standard.Agents.Brokers.Loggings;

namespace Standard.Agents.Services.Foundations.Knowledges;

public partial class KnowledgeService : IKnowledgeService
{
    private readonly IKnowledgeBroker knowledgeBroker;
    private readonly ILoggingBroker loggingBroker;

    public KnowledgeService(
        IKnowledgeBroker knowledgeBroker,
        ILoggingBroker loggingBroker)
    {
        this.knowledgeBroker = knowledgeBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<IReadOnlyList<string>> RetrieveKnowledgeAsync(string query) =>
    TryCatch(async () =>
    {
        ValidateQuery(query);

        return await this.knowledgeBroker.SelectKnowledgeAsync(query);
    });
    }
