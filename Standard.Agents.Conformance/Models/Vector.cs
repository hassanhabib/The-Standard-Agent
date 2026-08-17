// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Conformance;

public sealed record Vector(
    string Name,
    string? Description,
    List<string> GeneratorReplies,
    Dictionary<string, string>? Tools,
    string Prompt,
    Expectation Expect,

    // Guardian inputs — all optional, so pre-guardian vectors deserialize unchanged.
    // Constitution / Consumption are inline markdown the harness writes to a file next to
    // the executable and points the real .Constitution()/.Consumption() builder at, so the
    // file-resolution path is exercised, not bypassed. GateVerdict / JudgeScore drive the
    // scripted guardians (default "allow" / "1.0"), letting a vector force a refusal.
    string? Constitution = null,
    string? Consumption = null,
    string? GateVerdict = null,
    string? JudgeScore = null,

    // Multi-run vectors. The decision log's durability (SPEC.md §4.7) and run isolation
    // (§4.4) are only observable across more than one run, so a vector may drive several
    // prompts through one agent instead of one. Prompts overrides Prompt when present;
    // Concurrent runs them all at once rather than in order, which is the only way to
    // certify that one run's bookkeeping never leaks into another's.
    List<string>? Prompts = null,
    bool Concurrent = false);
