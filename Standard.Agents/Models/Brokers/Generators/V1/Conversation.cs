// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Brokers.Generators.V1;

/// <summary>
/// The V1 generator contract's shape: a conversation of typed messages, and tools declared as
/// data rather than described in prose.
/// </summary>
/// <remarks>
/// V0 sends one system prompt and one user prompt, and reads the model's choice out of the first
/// line of its text (<c>ACTION:</c> / <c>TOOL:</c> / <c>FINAL:</c>). That works everywhere, which
/// is why it stays the Core contract — but it forfeits what hosted models are actually trained on,
/// cannot express parallel calls, and has no way to tie a result back to the call that asked for
/// it. V1 exists for the providers that do better; V0 keeps working untouched, and the five
/// provider packages compile against it until they choose to move.
/// </remarks>
public enum MessageRole
{
    System,
    User,
    Assistant,

    /// <summary>A tool's result, tied to the call that asked for it by <c>ToolCallId</c>.</summary>
    Tool
}

public sealed record ConversationMessage
{
    public MessageRole Role { get; init; }
    public string Content { get; init; } = "";

    /// <summary>Calls the assistant asked for, when it asked for any.</summary>
    public IReadOnlyList<ModelToolCall> ToolCalls { get; init; } = [];

    /// <summary>Which call this message answers — set on a <see cref="MessageRole.Tool"/> message.</summary>
    public string ToolCallId { get; init; } = "";
}

/// <summary>A tool offered to the model, as data rather than as a sentence in a prompt.</summary>
public sealed record ToolDefinition(string Name, string Description, string ParametersJson);

/// <summary>
/// One call the model asked for. The id is what makes a result attributable to a request, which
/// is the thing the text protocol cannot express.
/// </summary>
public sealed record ModelToolCall(string Id, string Name, string ArgumentsJson);

/// <summary>
/// What a V1 generation produced: text, tool calls, or both — and what it cost, when the
/// provider says (SPEC.md §3.4).
/// </summary>
public sealed record GenerationResult
{
    public string Content { get; init; } = "";
    public IReadOnlyList<ModelToolCall> ToolCalls { get; init; } = [];

    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }

    public bool HasToolCalls => ToolCalls.Count > 0;
}
