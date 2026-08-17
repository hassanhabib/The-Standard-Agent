// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Knowledges;

// The Custom mode's substrate for knowledge (SPEC.md §4.8). Ranking is the host's to do here:
// the contract is "the passages relevant to this query, best first", and how relevance is
// computed is the broker's concern (§4.2, §9).
public sealed class FunctionKnowledgeBroker : IKnowledgeBroker
{
    private readonly Func<string, ValueTask<IReadOnlyList<string>>> retrieve;

    public FunctionKnowledgeBroker(Func<string, ValueTask<IReadOnlyList<string>>> retrieve) =>
        this.retrieve = retrieve;

    public ValueTask<IReadOnlyList<string>> SelectKnowledgeAsync(string query) =>
        this.retrieve(query);
}
