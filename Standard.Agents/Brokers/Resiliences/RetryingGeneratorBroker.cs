// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Generators;
using Standard.Agents.Models.Brokers.Generators;

namespace Standard.Agents.Brokers.Resiliences;

// Retry by decoration, not by a service holding a second broker.
//
// Resilience does not get logging's utility exemption. Logging is write-only observability and
// cannot change what the agent does; resilience decides whether the call happens again, which is
// control flow. A foundation holding a thing that re-runs its own broker is not "one broker, one
// resource" — so the wrapping happens at composition and BrainService never learns retry exists.
//
// A retried call is still one turn (SPEC.md §4.10): retries happen inside the call, so the loop's
// turn budget counts the agent's reasoning rather than the network's luck.
public sealed class RetryingGeneratorBroker : IGeneratorBroker
{
    private readonly IGeneratorBroker generatorBroker;
    private readonly IResilienceBroker resilienceBroker;

    public RetryingGeneratorBroker(
        IGeneratorBroker generatorBroker,
        IResilienceBroker resilienceBroker)
    {
        this.generatorBroker = generatorBroker;
        this.resilienceBroker = resilienceBroker;
    }

    // A decorator that leaned on the interface's default member would silently drop the resolved
    // options before the wire — the default degrades on the DECORATOR, not on the broker it
    // wraps. Every pass-through here is explicit for that reason, and so is the flag.
    public bool HonorsRequest => this.generatorBroker.HonorsRequest;

    public ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt) =>
        this.resilienceBroker.ExecuteAsync(() =>
            this.generatorBroker.GenerateAsync(systemPrompt, userPrompt));

    public ValueTask<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        ResolvedInference inference) =>
        this.resilienceBroker.ExecuteAsync(() =>
            this.generatorBroker.GenerateAsync(systemPrompt, userPrompt, inference));

    // Streaming passes straight through, exactly as it did before. A stream that has already
    // yielded tokens cannot be retried from the start without the caller seeing them twice, and
    // silently replaying a partial answer is worse than surfacing the failure.
    public IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default) =>
        this.generatorBroker.GenerateStreamAsync(systemPrompt, userPrompt, cancellationToken);

    public IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        ResolvedInference inference,
        CancellationToken cancellationToken) =>
        this.generatorBroker.GenerateStreamAsync(
            systemPrompt, userPrompt, inference, cancellationToken);
}
