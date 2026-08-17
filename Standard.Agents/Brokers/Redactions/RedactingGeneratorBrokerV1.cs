// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Generators;
using Standard.Agents.Models.Brokers.Generators.V1;

namespace Standard.Agents.Brokers.Redactions;

// The same guarantee on the native path. §4.6 says every model call, and a second contract is
// still a model call — the tool-call arguments coming back are rehydrated too, because a value
// the model echoed into an argument is a value that reaches a tool.
public sealed class RedactingGeneratorBrokerV1 : IGeneratorBrokerV1
{
    private readonly IGeneratorBrokerV1 generatorBroker;
    private readonly IRedactionBroker redactionBroker;

    public RedactingGeneratorBrokerV1(
        IGeneratorBrokerV1 generatorBroker,
        IRedactionBroker redactionBroker)
    {
        this.generatorBroker = generatorBroker;
        this.redactionBroker = redactionBroker;
    }

    public async ValueTask<GenerationResult> GenerateAsync(
        IReadOnlyList<ConversationMessage> messages,
        IReadOnlyList<ToolDefinition> tools)
    {
        var vault = new Dictionary<string, string>();

        IReadOnlyList<ConversationMessage> redacted =
            [.. messages.Select(message => message with
            {
                Content = this.redactionBroker.Redact(message.Content, vault)
            })];

        GenerationResult result = await this.generatorBroker.GenerateAsync(redacted, tools);

        return result with
        {
            Content = this.redactionBroker.Rehydrate(result.Content, vault),

            ToolCalls =
            [
                .. result.ToolCalls.Select(call => call with
                {
                    ArgumentsJson = this.redactionBroker.Rehydrate(call.ArgumentsJson, vault)
                })
            ]
        };
    }
}
