// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Decision;

namespace Standard.Agents.Services.Foundations.Decision;

public sealed class BrainService : IBrainService
{
    private readonly IGeneratorBroker generatorBroker;

    public BrainService(IGeneratorBroker generatorBroker) =>
        this.generatorBroker = generatorBroker;

    public async ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt) =>
        await this.generatorBroker.GenerateAsync(systemPrompt, userPrompt);
}
