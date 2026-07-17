// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Decision;
using Standard.Agents.Brokers.Loggings;

namespace Standard.Agents.Services.Foundations.Decision;

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

    public ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt) =>
    TryCatch(async () =>
    {
        ValidateUserPrompt(userPrompt);

        return await this.generatorBroker.GenerateAsync(systemPrompt, userPrompt);
    });
}
