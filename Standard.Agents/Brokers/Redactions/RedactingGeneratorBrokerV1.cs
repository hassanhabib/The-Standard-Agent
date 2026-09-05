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
// the model echoed into an argument is a value that reaches a tool. And the whole message goes
// out redacted, not only its prose: a replayed tool call carries arguments that were rehydrated
// for the tool on an earlier turn, so they are tokenized again like the text around them. The
// 2026-09-04 principal review (F-02) found them going out in the clear.
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

        IReadOnlyList<ConversationMessage> redactedMessages =
            [.. messages.Select(message => RedactMessage(message, vault))];

        GenerationResult result = inference is null
            ? await this.generatorBroker.GenerateAsync(redactedMessages, tools)
            : await this.generatorBroker.GenerateAsync(redactedMessages, tools, inference);

        return RehydrateResult(result, vault);
    }

    private ConversationMessage RedactMessage(
        ConversationMessage message,
        IDictionary<string, string> vault)
    {
        return message with
        {
            Content = this.redactionBroker.Redact(message.Content, vault),
            ToolCalls = [.. message.ToolCalls.Select(toolCall => RedactToolCall(toolCall, vault))]
        };
    }

    private ModelToolCall RedactToolCall(ModelToolCall toolCall, IDictionary<string, string> vault) =>
        toolCall with { ArgumentsJson = this.redactionBroker.Redact(toolCall.ArgumentsJson, vault) };

    private GenerationResult RehydrateResult(
        GenerationResult result,
        IReadOnlyDictionary<string, string> vault)
    {
        return result with
        {
            Content = this.redactionBroker.Rehydrate(result.Content, vault),
            ToolCalls = [.. result.ToolCalls.Select(toolCall => RehydrateToolCall(toolCall, vault))]
        };
    }

    private ModelToolCall RehydrateToolCall(
        ModelToolCall toolCall,
        IReadOnlyDictionary<string, string> vault)
    {
        return toolCall with
        {
            ArgumentsJson = this.redactionBroker.Rehydrate(toolCall.ArgumentsJson, vault)
        };
    }
}
