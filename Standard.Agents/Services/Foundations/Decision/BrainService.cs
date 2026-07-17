// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Decision;
using Standard.Agents.Brokers.Loggings;

namespace Standard.Agents.Services.Foundations.Decision;

// The one reasoning engine — the singular waist of the hourglass. An agent has one
// brain; multiplicity of brains is the fractal, not an intra-agent feature.
public partial class BrainService : IBrainService
{
    private readonly IGeneratorBroker generatorBroker;
    private readonly ILoggingBroker loggingBroker;

    public BrainService(
        IGeneratorBroker generatorBroker,
        ILoggingBroker loggingBroker)
    {
        this.generatorBroker = generatorBroker;
        this.loggingBroker = loggingBroker;
    }

    public async ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt) =>
        await this.generatorBroker.GenerateAsync(systemPrompt, userPrompt);
}
