// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Foundations.Brains;

namespace Standard.Agents.Services.Foundations.Brains;

public partial class BrainService : IBrainService
{
    private readonly IGeneratorBroker generatorBroker;
    private readonly ILoggingBroker loggingBroker;
    private readonly IReadOnlyList<RedactionRule> redactionRules;

    public BrainService(
        IGeneratorBroker generatorBroker,
        ILoggingBroker loggingBroker,
        IEnumerable<RedactionRule>? redactionRules = null)
    {
        this.generatorBroker = generatorBroker;
        this.loggingBroker = loggingBroker;
        this.redactionRules = redactionRules?.ToList() ?? [];
    }

    public ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt) =>
    TryCatch(async () =>
    {
        ValidateUserPrompt(userPrompt);

        var vault = new Dictionary<string, string>();
        string redactedSystemPrompt = Redact(systemPrompt, vault);
        string redactedUserPrompt = Redact(userPrompt, vault);

        string reply = await this.generatorBroker.GenerateAsync(
            redactedSystemPrompt, redactedUserPrompt);

        return Rehydrate(reply, vault);
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
