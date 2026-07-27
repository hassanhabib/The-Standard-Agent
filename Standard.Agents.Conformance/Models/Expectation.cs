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
    List<string>? GuardianRubricExcludes = null);
