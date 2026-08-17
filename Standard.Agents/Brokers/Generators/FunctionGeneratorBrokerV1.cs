// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Brokers.Generators.V1;

namespace Standard.Agents.Brokers.Generators;

// The Custom mode for the V1 seam: a host-supplied conversation handler. Also what a test
// scripts, which is how native tool calling is certified without a provider.
public sealed class FunctionGeneratorBrokerV1 : IGeneratorBrokerV1
{
    private readonly Func<
        IReadOnlyList<ConversationMessage>,
        IReadOnlyList<ToolDefinition>,
        ValueTask<GenerationResult>> generate;

    public FunctionGeneratorBrokerV1(
        Func<
            IReadOnlyList<ConversationMessage>,
            IReadOnlyList<ToolDefinition>,
            ValueTask<GenerationResult>> generate) =>
        this.generate = generate;

    public ValueTask<GenerationResult> GenerateAsync(
        IReadOnlyList<ConversationMessage> messages,
        IReadOnlyList<ToolDefinition> tools) =>
        this.generate(messages, tools);
}
