// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Brokers.Generators;

namespace Standard.Agents.Services.Foundations.Brains;

// One broker role, versioned. Redaction and retry used to be held here as two more brokers; both
// are now decorations applied to the generator at composition, so this service knows about
// neither and cannot forget either (docs/architecture-alignment.md).
public partial class BrainService : IBrainService
{
    private readonly IGeneratorBroker generatorBroker;
    private readonly ILoggingBroker loggingBroker;
    private readonly IGeneratorBrokerV1? generatorBrokerV1;

    /// <summary>True when a V1 brain is configured and native tool calling is available.</summary>
    public bool SpeaksNatively => this.generatorBrokerV1 is not null;

    public BrainService(
        IGeneratorBroker generatorBroker,
        ILoggingBroker loggingBroker,
        IGeneratorBrokerV1? generatorBrokerV1 = null)
    {
        this.generatorBroker = generatorBroker;
        this.loggingBroker = loggingBroker;
        this.generatorBrokerV1 = generatorBrokerV1;
    }

    public ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt) =>
    TryCatch(async () =>
    {
        ValidateUserPrompt(userPrompt);

        return await this.generatorBroker.GenerateAsync(systemPrompt, userPrompt);
    });

    // Stops discarding what it was handed (docs/per-request-inference.md §2): the resolved
    // options ride through to the broker, which honors them or degrades to the plain call by
    // its own default member — either way the guardian still holds the answer to shape.
    public ValueTask<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        ResolvedInference? inference) =>
    TryCatch(async () =>
    {
        ValidateUserPrompt(userPrompt);

        return inference is null
            ? await this.generatorBroker.GenerateAsync(systemPrompt, userPrompt)
            : await this.generatorBroker.GenerateAsync(systemPrompt, userPrompt, inference);
    });

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerator<string> tokens;

        try
        {
            ValidateUserPrompt(userPrompt);

            tokens = this.generatorBroker
                .GenerateStreamAsync(systemPrompt, userPrompt, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception exception)
        {
            throw await MapStreamExceptionAsync(exception);
        }

        try
        {
            while (true)
            {
                string token;

                try
                {
                    if (await tokens.MoveNextAsync() is false)
                    {
                        break;
                    }

                    token = tokens.Current;
                }
                catch (Exception exception)
                {
                    throw await MapStreamExceptionAsync(exception);
                }

                yield return token;
            }
        }
        finally
        {
            await tokens.DisposeAsync();
        }
    }
}
