// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Generators;
using Standard.Agents.Models.Brokers.Generators.V1;

namespace Standard.Agents.Brokers.Resiliences;

// The same on the native path. A control that holds on one protocol and not the other is a
// control a caller can step around by changing configuration.
public sealed class RetryingGeneratorBrokerV1 : IGeneratorBrokerV1
{
    private readonly IGeneratorBrokerV1 generatorBroker;
    private readonly IResilienceBroker resilienceBroker;

    public RetryingGeneratorBrokerV1(
        IGeneratorBrokerV1 generatorBroker,
        IResilienceBroker resilienceBroker)
    {
        this.generatorBroker = generatorBroker;
        this.resilienceBroker = resilienceBroker;
    }

    public ValueTask<GenerationResult> GenerateAsync(
        IReadOnlyList<ConversationMessage> messages,
        IReadOnlyList<ToolDefinition> tools) =>
        this.resilienceBroker.ExecuteAsync(() =>
            this.generatorBroker.GenerateAsync(messages, tools));
}
