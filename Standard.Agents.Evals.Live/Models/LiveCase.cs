// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Evals.Live;

// One live case: a pinned skillset from the PeerLLM registry, the door the prompts enter by,
// the tools on offer, the prompts with their golden data, how many times each is sampled, and
// the pass rate each metric must reach. Pinned means pinned: a skillset without @version is
// refused, because a score nobody can reproduce is a score nobody can investigate.
[System.Text.Json.Serialization.JsonUnmappedMemberHandling(
    System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow)]
public sealed record LiveCase(
    string Name,
    string? Description,
    string Skillset,
    List<LivePrompt> Prompts,
    Dictionary<string, double> Thresholds,

    // Which members of the skillset to load; empty loads them all. A skillset can be far
    // larger than a system prompt should be, and a case measures one skill at a time.
    List<string>? Members = null,

    // "text" rides the text protocol (Brain); "native" rides tool calling (NativeBrain).
    string Protocol = "text",
    Dictionary<string, string>? Tools = null,
    int Samples = 3,
    int MaxTurns = 4,

    // The cost control: tokens a single run may spend before the budget stops it.
    int MaxTokensPerRun = 12000);
