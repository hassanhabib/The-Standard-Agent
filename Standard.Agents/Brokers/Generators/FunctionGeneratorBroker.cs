// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Standard.Agents.Brokers.Generators;

public sealed class FunctionGeneratorBroker : IGeneratorBroker
{
    private readonly Func<string, string, ValueTask<string>> generate;

    public FunctionGeneratorBroker(Func<string, string, ValueTask<string>> generate) =>
        this.generate = generate;

    public async ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt) =>
        await this.generate(systemPrompt, userPrompt);

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return await this.generate(systemPrompt, userPrompt);
    }
}
