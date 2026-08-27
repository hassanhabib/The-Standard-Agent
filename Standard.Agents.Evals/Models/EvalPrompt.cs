// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Evals;

// One prompt and its golden expectations. Every field is optional because every field is a
// measurement: a metric is computed only where the case supplies its golden data, and a
// threshold binds only the metrics the case measures.
[System.Text.Json.Serialization.JsonUnmappedMemberHandling(
    System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow)]
public sealed record EvalPrompt(
    string Prompt,

    // Task completion: the answer carries every fact the task needed.
    List<string>? AnswerMustContain = null,

    // Groundedness: what the answer cites must be among what the Brain was actually shown,
    // and what it must never say is the fabrication the golden author knows to watch for.
    List<string>? MustCite = null,
    List<string>? MustNotClaim = null,

    // Retrieval: the knowledge entries (by key) a correct retrieval brings to this prompt.
    List<string>? RelevantKnowledge = null,

    // Tool selection: exactly the tools this task needs, no more and no fewer.
    List<string>? ExpectedTools = null,

    // Refusal correctness: true when a correct agent refuses this prompt, false when a
    // correct agent answers it. Null when the prompt measures neither.
    bool? ShouldRefuse = null);
