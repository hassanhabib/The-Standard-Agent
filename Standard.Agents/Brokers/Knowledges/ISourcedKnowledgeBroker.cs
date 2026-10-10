// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Knowledges;

namespace Standard.Agents.Brokers.Knowledges;

// The knowledge seam for a source that can say where a passage came from (SPEC.md §4.1). It sits
// beside IKnowledgeBroker rather than replacing it: a source implements either, and a plain one
// keeps working, lifted to passages with no known origin.
public interface ISourcedKnowledgeBroker
{
    ValueTask<IReadOnlyList<KnowledgeResult>> SelectSourcedKnowledgeAsync(string query);
}
