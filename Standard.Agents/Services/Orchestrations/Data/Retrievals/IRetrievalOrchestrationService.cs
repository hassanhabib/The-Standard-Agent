// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Services.Orchestrations.Data.Retrievals;

// Authored material, selected by relevance.
//
// Skills and knowledge share a region because they share a concept: both are written by someone,
// both are stored, and both are chosen for this prompt by how well they match it. That is not
// obvious until you notice that Recall does the same thing to each of them.
public interface IRetrievalOrchestrationService
{
    /// <summary>
    /// The instructions for this prompt — the applicable skills, with the tool and skill catalog
    /// markers expanded.
    /// </summary>
    ValueTask<string> RetrieveInstructionsAsync(string route);

    /// <summary>The passages that answer this prompt, best first.</summary>
    ValueTask<IReadOnlyList<string>> RetrieveGroundingAsync(string query);
}
