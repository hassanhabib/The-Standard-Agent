// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Generators;
using Standard.Agents.Models.Brokers.Generators.V1;

namespace Standard.Agents.Conformance;

// A scripted V1 model: each reply is either a tool call or an answer, taken in order.
//
// It records the conversation it was handed on every call, because that is what the round-trip
// vector actually asserts. A generator that only returned scripted replies could not tell a
// framework that hands back a tool message naming the call from one that narrates the result as
// prose — and the difference between those two is the whole point of native tool calling.
public sealed class ScriptedNativeGeneratorBroker : IGeneratorBrokerV1
{
    private readonly IReadOnlyList<NativeReply> replies;
    private readonly List<IReadOnlyList<ConversationMessage>> conversations = [];
    private int index;

    public ScriptedNativeGeneratorBroker(IReadOnlyList<NativeReply> replies) =>
        this.replies = replies;

    public IReadOnlyList<IReadOnlyList<ConversationMessage>> Conversations => this.conversations;

    public IReadOnlyList<ToolDefinition> LastTools { get; private set; } = [];

    public async ValueTask<GenerationResult> GenerateAsync(
        IReadOnlyList<ConversationMessage> messages,
        IReadOnlyList<ToolDefinition> tools)
    {
        this.conversations.Add(messages);
        LastTools = tools;

        NativeReply reply = this.index < this.replies.Count
            ? this.replies[this.index++]
            : new NativeReply(Content: "done");

        return new GenerationResult
        {
            Content = reply.Content ?? string.Empty,

            ToolCalls =
            [
                .. (reply.ToolCalls ?? []).Select(call =>
                    new ModelToolCall(call.Id, call.Name, call.ArgumentsJson))
            ],

            PromptTokens = reply.PromptTokens,
            CompletionTokens = reply.CompletionTokens
        };
    }
}

/// <summary>One scripted V1 reply, as a vector writes it.</summary>
// A typo'd field must fail loudly, not silently delete the assertion or the input it
// carried: a vector that asserts less than it appears to reads as coverage (SPEC.md §1.1).
[System.Text.Json.Serialization.JsonUnmappedMemberHandling(
    System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow)]
public sealed record NativeReply(
    string? Content = null,
    List<NativeToolCall>? ToolCalls = null,
    int PromptTokens = 0,
    int CompletionTokens = 0);

public sealed record NativeToolCall(string Id, string Name, string ArgumentsJson);
