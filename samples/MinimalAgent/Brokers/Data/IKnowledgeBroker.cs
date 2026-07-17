// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace MinimalAgent.Brokers.Data;

public interface IKnowledgeBroker
{
    ValueTask<IReadOnlyList<string>> SelectKnowledgeAsync(string query);
}
