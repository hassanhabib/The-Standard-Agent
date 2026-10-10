// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Knowledges;

namespace Standard.Agents.Services.Foundations.Knowledges;

public interface IKnowledgeService
{
    ValueTask<IReadOnlyList<string>> RetrieveKnowledgeAsync(string query);

    /// <summary>
    /// The same passages, in the same order, each with the score it ranked by and the source it
    /// came from (SPEC.md §3.7, §4.2). A broker that cannot say where a passage came from yields
    /// passages with no source, which are never cited.
    /// </summary>
    ValueTask<IReadOnlyList<KnowledgeResult>> RetrieveSourcedKnowledgeAsync(string query);
}
