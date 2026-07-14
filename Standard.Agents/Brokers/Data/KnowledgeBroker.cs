// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Data;

public sealed class KnowledgeBroker : IKnowledgeBroker
{
    public ValueTask<IReadOnlyList<string>> SelectKnowledgeAsync(string query) =>
        ValueTask.FromResult<IReadOnlyList<string>>([]);
}
