// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Generators;

namespace Standard.Agents.Brokers.Generators;

public interface IGeneratorBroker
{
    ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt);

    IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// True when this broker puts resolved inference options on the wire. A broker that has not
    /// opted in silently ignores them — and the answer is still held to shape, because the
    /// Contract guardian validates and revises regardless (docs/per-request-inference.md §5).
    /// Constrained decoding is an optimization over a guarantee the architecture already provides;
    /// this flag exists so the trace can say which one happened.
    /// </summary>
    bool HonorsRequest => false;

    /// <summary>
    /// The request-carrying call: the same generation, with the run's resolved inference options
    /// — precedence has already been applied at the boundary, so a broker writes what it is
    /// given and decides nothing. Default: degrade to the plain call, so the five provider
    /// packages keep compiling and opt in on their own schedule.
    /// </summary>
    ValueTask<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        ResolvedInference inference) =>
        GenerateAsync(systemPrompt, userPrompt);

    /// <summary>
    /// The streamed twin. A control a caller can step around by changing method is not a control
    /// (SPEC.md §7.6) — the streamed loop carries the same resolved options the batched one does.
    /// </summary>
    IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        ResolvedInference inference,
        CancellationToken cancellationToken = default) =>
        GenerateStreamAsync(systemPrompt, userPrompt, cancellationToken);
}
