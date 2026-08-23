// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Conformance;

// A typo'd field must fail loudly, not silently delete the assertion or the input it
// carried: a vector that asserts less than it appears to reads as coverage (SPEC.md §1.1).
[System.Text.Json.Serialization.JsonUnmappedMemberHandling(
    System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow)]
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
    int? GateScreenedPromptTimes = null,

    //   CompensationOrder — the exact order the tools were reversed in, which is how the reverse
    //                       unwind is certified: a later effect may depend on an earlier one, so
    //                       undoing them in the order they ran would leave state inconsistent.
    List<string>? CompensationOrder = null,

    //   ToolResultAnswersCall — the tool's result must come back to the model as a tool message
    //                           naming the call that asked for it, and the assistant's request
    //                           must be replayed alongside it. This is the one thing the text
    //                           protocol cannot express, so it is the one thing worth certifying
    //                           about native tool calling (SPEC.md §6).
    string? ToolResultAnswersCall = null,

    //   ResultsContain — one entry per prompt, in order: what each PROMPT's result must carry.
    //                     The single Result/ResultContains sees only the last prompt, so a
    //                     multi-prompt vector could misbehave on every run but its final one and
    //                     still print PASS — which is how the approval-resume vector was
    //                     deletion-blind.
    List<string>? ResultsContain = null,

    //   PolicySawPrincipal — the identity the policy broker was actually handed when it decided.
    //                        An audit record naming the caller afterwards is not authorization,
    //                        so this reads the decision's input rather than the log (SPEC.md §4.9).
    string? PolicySawPrincipal = null);
