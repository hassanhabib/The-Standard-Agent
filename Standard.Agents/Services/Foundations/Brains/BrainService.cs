// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Text;
using Standard.Agents.Brokers.Generators;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Brokers.Redactions;
using Standard.Agents.Brokers.Resiliences;

namespace Standard.Agents.Services.Foundations.Brains;

public partial class BrainService : IBrainService
{
    private readonly IGeneratorBroker generatorBroker;
    private readonly ILoggingBroker loggingBroker;
    private readonly IRedactionBroker redactionBroker;
    private readonly IResilienceBroker resilienceBroker;

    public BrainService(
        IGeneratorBroker generatorBroker,
        ILoggingBroker loggingBroker,
        IRedactionBroker? redactionBroker = null,
        IResilienceBroker? resilienceBroker = null)
    {
        this.generatorBroker = generatorBroker;
        this.loggingBroker = loggingBroker;
        this.redactionBroker = redactionBroker ?? new NotConfiguredRedactionBroker();
        this.resilienceBroker = resilienceBroker ?? new NotConfiguredResilienceBroker();
    }

    public ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt) =>
    TryCatch(async () =>
    {
        ValidateUserPrompt(userPrompt);

        var vault = new Dictionary<string, string>();
        string redactedSystemPrompt = this.redactionBroker.Redact(systemPrompt, vault);
        string redactedUserPrompt = this.redactionBroker.Redact(userPrompt, vault);

        // A retried call is still one turn (SPEC.md §4.10) — retries happen inside the call, so
        // the loop's turn budget counts the agent's reasoning, not the network's luck.
        string reply = await this.resilienceBroker.ExecuteAsync(() =>
            this.generatorBroker.GenerateAsync(redactedSystemPrompt, redactedUserPrompt));

        return this.redactionBroker.Rehydrate(reply, vault);
    });

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IAsyncEnumerator<string> tokens;
        var vault = new Dictionary<string, string>();

        try
        {
            ValidateUserPrompt(userPrompt);

            string redactedSystemPrompt = this.redactionBroker.Redact(systemPrompt, vault);
            string redactedUserPrompt = this.redactionBroker.Redact(userPrompt, vault);

            tokens = this.generatorBroker
                .GenerateStreamAsync(redactedSystemPrompt, redactedUserPrompt, cancellationToken)
                .GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception exception)
        {
            throw await MapStreamExceptionAsync(exception);
        }

        var pending = new StringBuilder();

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

                if (vault.Count == 0)
                {
                    yield return token;

                    continue;
                }

                pending.Append(token);
                string ready = DrainReady(pending, vault, this.redactionBroker);

                if (ready.Length > 0)
                {
                    yield return ready;
                }
            }

            if (vault.Count > 0 && pending.Length > 0)
            {
                yield return this.redactionBroker.Rehydrate(pending.ToString(), vault);
            }
        }
        finally
        {
            await tokens.DisposeAsync();
        }
    }
}
