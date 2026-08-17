// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Conformance;

public sealed record Expectation(
    string? Result,
    string? ResultContains,
    Dictionary<string, string>? ToolInput,

    // Guardian-rubric assertions. Each substring must appear in (Contains) — or be absent
    // from (Excludes) — BOTH the composed Gate rubric AND the composed Judge rubric, since a
    // constitution binds both guardians and a consumption skill replaces the policy in both.
    List<string>? GuardianRubricContains = null,
    List<string>? GuardianRubricExcludes = null,

    // Decision-log assertions (SPEC.md §4.7, §4.4), observed through the log's Custom sink so
    // the certification does not depend on any particular storage.
    //
    //   AuditRunCount              — how many distinct runs the log must hold: one per prompt,
    //                                never fewer. Fewer means runs were merged or discarded.
    //   AuditRetainsEveryPrompt    — every prompt's evidence must still be present at the end,
    //                                which is what fails when beginning a run truncates the log.
    //   AuditSequencesUniquePerRun — within a run the record numbers must not repeat, which is
    //                                what fails when run counters are shared between runs.
    int? AuditRunCount = null,
    bool AuditRetainsEveryPrompt = false,
    bool AuditSequencesUniquePerRun = false,

    // Guardian-input assertions (SPEC.md §4.2, §7.6).
    //
    //   JudgeSawTask         — the task must have reached the Judge, not the candidate alone.
    //   GuardianNeverAnswers — a guardian that tries to answer or act must be neutralized, and
    //                          its text must never become the agent's result.
    bool JudgeSawTask = false,
    bool GuardianNeverAnswers = false,

    //   NoModelSees - this text must not appear in ANY model call: not the Brain's prompt,
    //                 not the Gate's input, not the Judge's. Redaction that covers one call
    //                 and not the others does not satisfy SPEC.md 4.6.
    string? NoModelSees = null,

    // Perimeter assertions (SPEC.md §4.9, §7.7).
    //
    //   ToolNeverRan    — these tools must not have executed at all. Held is not performed.
    //   ToolRunCount    — exactly how many times each tool executed, which is how run-once is
    //                     certified: proposing an act three times must still run it once.
    //   BrainNeverSees  — this text must never reach the Brain, however it entered as Data.
    List<string>? ToolNeverRan = null,
    Dictionary<string, int>? ToolRunCount = null,
    string? BrainNeverSees = null,

    //   BrainSees                — this text must have reached the Brain, which is how retrieval
    //                              is certified: the passage that answers the question got there.
    //   GateScreenedPromptTimes  — exactly how many times the Gate was asked about the prompt.
    string? BrainSees = null,
    int? GateScreenedPromptTimes = null);
