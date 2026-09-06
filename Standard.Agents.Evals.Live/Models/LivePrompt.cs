// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Evals.Live;

[System.Text.Json.Serialization.JsonUnmappedMemberHandling(
    System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow)]
public sealed record LivePrompt(
    string Prompt,

    // Task completion: every one of these is in the answer (case-insensitive, because a live
    // model's casing is not the fact under test).
    List<string>? AnswerMustContain = null,

    // Task completion, the looser form: at least one of these is in the answer.
    List<string>? AnswerMustContainAny = null,

    // Groundedness: none of these is in the answer - the fabrication the author knows to watch for.
    List<string>? AnswerMustNotContain = null,

    // Tool selection: exactly these tools ran, no more and no fewer.
    List<string>? ExpectedTools = null);
