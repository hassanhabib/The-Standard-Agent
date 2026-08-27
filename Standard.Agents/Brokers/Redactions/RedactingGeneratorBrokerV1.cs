// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Generators;
using Standard.Agents.Models.Brokers.Generators;
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

    // Explicit pass-throughs, never the interface's defaults: a decorator that leaned on the
    // default member would degrade HERE and silently drop the resolved options before the wire.
    public bool HonorsRequest => this.generatorBroker.HonorsRequest;

    public ValueTask<GenerationResult> GenerateAsync(
        IReadOnlyList<ConversationMessage> messages,
        IReadOnlyList<ToolDefinition> tools) =>
        GenerateAsync(messages, tools, inference: null);

    ValueTask<GenerationResult> IGeneratorBrokerV1.GenerateAsync(
        IReadOnlyList<ConversationMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        ResolvedInference inference) =>
        GenerateAsync(messages, tools, inference);

    private async ValueTask<GenerationResult> GenerateAsync(
        IReadOnlyList<ConversationMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        ResolvedInference? inference)
    {
        var vault = new Dictionary<string, string>();

        IReadOnlyList<ConversationMessage> redacted =
            [.. messages.Select(message => message with
            {
                Content = this.redactionBroker.Redact(message.Content, vault)
            })];

        GenerationResult result = inference is null
            ? await this.generatorBroker.GenerateAsync(redacted, tools)
            : await this.generatorBroker.GenerateAsync(redacted, tools, inference);

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
