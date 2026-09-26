// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Orchestrations.Agents;

/// <summary>
/// One native tool call and its result, kept together.
/// </summary>
/// <remarks>
/// The id is the whole point. A hosted model that asked for a call expects the answer back as a
/// tool message naming that call — that is what it was trained on, and it is the one thing the
/// text protocol cannot express (SPEC.md §6). Replaying the result as narrated prose instead
/// leaves the model to match answers to questions by reading, which is exactly the guessing
/// native tool calling exists to remove.
/// </remarks>
public sealed record ToolExchange(
    string CallId,
    string ToolName,
    string ArgumentsJson,
    string Result)
{
    /// <summary>
    /// Whether the run-once ledger answered this call rather than the tool (SPEC.md §3.2, v1.14).
    /// False by default, so an exchange recorded before this existed reads as a call that ran. A
    /// loop that counts repeated asks has to tell the two apart: a read after an edit is the same
    /// ask and is not a repeat.
    /// </summary>
    public bool Replayed { get; init; }
}
