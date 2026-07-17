// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Knowledges;

public interface IKnowledgeBroker
{
    ValueTask<IReadOnlyList<string>> SelectKnowledgeAsync(string query);
}
