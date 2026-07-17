// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Knowledges;

namespace Standard.Agents.Conformance;

public sealed class StubKnowledgeBroker : IKnowledgeBroker
{
    public ValueTask<IReadOnlyList<string>> SelectKnowledgeAsync(string query) =>
        ValueTask.FromResult<IReadOnlyList<string>>([]);
    }
