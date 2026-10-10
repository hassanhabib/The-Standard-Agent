// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Knowledges;

namespace Standard.Agents.Brokers.Knowledges;

// The Custom mode's substrate for sourced knowledge (SPEC.md §4.8): the host's own retrieval,
// ranked by the host, saying where each passage came from.
public sealed class FunctionSourcedKnowledgeBroker : ISourcedKnowledgeBroker
{
    private readonly Func<string, ValueTask<IReadOnlyList<KnowledgeResult>>> retrieve;

    public FunctionSourcedKnowledgeBroker(
        Func<string, ValueTask<IReadOnlyList<KnowledgeResult>>> retrieve) =>
        this.retrieve = retrieve;

    public ValueTask<IReadOnlyList<KnowledgeResult>> SelectSourcedKnowledgeAsync(string query) =>
        this.retrieve(query);
}
