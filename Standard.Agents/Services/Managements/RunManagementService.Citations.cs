// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Clients.Agents;
using Standard.Agents.Models.Orchestrations.Agents;

namespace Standard.Agents.Services.Managements;

// A grounded answer cites its sources (SPEC.md §4.2).
//
// The loop cites, not the model. A model asked to name its sources names them when it feels like
// it, so the citation is written here, from the sources Recall put on the run, once the run has
// answered — after the Judge, which scores the model's answer and not lines the agent wrote, and
// before the Response event and the session write, so every door carries the same text.
public partial class RunManagementService
{
    // Precedence, as every request field obeys it: what the deployment configured, then what the
    // request asked for, then off (docs/per-request-inference.md §4). A deployment that must cite
    // cannot be switched off by a caller, and one that never cites cannot be switched on.
    private bool IsCitingKnowledge(PromptRequest request) =>
        this.configuredCiteKnowledge ?? request.CiteKnowledge ?? false;

    // Only an answer is cited: a run that refused, asked, is waiting or failed has nothing to
    // credit. Nor is an answer held to a response schema, because a line after it breaks the very
    // shape the caller was promised. A source the answer already credits is not repeated.
    private AgentContext WithCitations(AgentContext context)
    {
        if (context.Status is not AgentStatus.Responded
            || string.IsNullOrWhiteSpace(context.Inference?.ResponseSchemaJson) is false)
        {
            return context;
        }

        List<string> citations = [.. context.GroundingSources
            .Select(source => this.knowledgeCitationPrefix + source)
                .Where(citation => context.Result.Contains(
                    citation, StringComparison.OrdinalIgnoreCase) is false)];

        if (citations.Count == 0)
        {
            return context;
        }

        string citedResult =
            $"{context.Result.TrimEnd()}\n\n{string.Join("\n", citations)}";

        return context with { Result = citedResult };
    }
}
