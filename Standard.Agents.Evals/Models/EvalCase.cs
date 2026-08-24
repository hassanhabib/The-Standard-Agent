// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Evals;

// One golden case: an agent composition, a deterministic script, and what a good agent does
// with it. A typo'd field must fail loudly rather than silently delete the expectation it
// carried — the same rule the conformance harness learned the hard way.
[System.Text.Json.Serialization.JsonUnmappedMemberHandling(
    System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow)]
public sealed record EvalCase(
    string Name,
    string? Description,
    List<string> GeneratorReplies,
    List<EvalPrompt> Prompts,
    Dictionary<string, double> Thresholds,

    // The composition under evaluation. Skill is the system prompt's identity; knowledge is
    // written to files and served through the REAL local retrieval (ranked lexical, floored),
    // so the retrieval being measured is the framework's, not the harness's.
    string Skill = "You are a careful assistant.",
    List<string>? Memories = null,
    Dictionary<string, string>? Knowledge = null,
    int KnowledgeMaxResults = 3,
    double KnowledgeMinScore = 0.0,
    Dictionary<string, string>? Tools = null,

    // Scripted guardians, consumed in order per verdict asked. Defaults are inert - an
    // always-allowing gate and an always-approving judge - so a case that sets neither
    // evaluates the agent, not the guardians.
    List<string>? GateVerdicts = null,
    List<string>? JudgeScores = null);
