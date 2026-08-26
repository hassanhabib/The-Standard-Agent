// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Generators;

namespace Standard.Agents.Services.Foundations.Brains;

public interface IBrainService
{
    ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt);

    /// <summary>
    /// The request-carrying call: the same generation with the run's resolved inference options
    /// (docs/per-request-inference.md §5). Null means the context was built by hand and carried
    /// no opinions — exactly the plain call.
    /// </summary>
    ValueTask<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        ResolvedInference? inference) =>
        GenerateAsync(systemPrompt, userPrompt);

    /// <summary>True when a V1 brain is configured and native tool calling is available.</summary>
    bool SpeaksNatively => false;

    /// <summary>
    /// The V1 path: a conversation in, a structured choice out. Only called when
    /// <see cref="SpeaksNatively"/> — the text protocol remains the Core contract.
    /// </summary>
    ValueTask<Models.Brokers.Generators.V1.GenerationResult> GenerateAsync(
        Models.Orchestrations.Agents.AgentContext context,
        IReadOnlyList<Models.Brokers.Generators.V1.ToolDefinition> tools) =>
        throw new NotSupportedException("This brain does not speak natively.");

    IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
