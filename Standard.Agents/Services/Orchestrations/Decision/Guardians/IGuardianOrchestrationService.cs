// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Judges;

namespace Standard.Agents.Services.Orchestrations.Decision.Guardians;

// A conscience, before and after.
//
// The Gate screens what goes in and the Judge scores what comes out. They are one concept at two
// moments, which is why they share a region: the framework already calls them the guardians,
// composes one rubric for both, and Invariant 6 treats them as a single class of thing.
public interface IGuardianOrchestrationService
{
    /// <summary>
    /// Screens the task. The same unchanged prompt is screened <b>once per run</b>, not once per
    /// turn — asking the identical question seven times at full model cost is waste, not safety
    /// (SPEC.md §4.10).
    /// </summary>
    ValueTask<string> ScreenAsync(string prompt);

    /// <summary>Looks for a direct contradiction across the active skill instructions.</summary>
    ValueTask<string> DetectConflictAsync(string instructions);

    /// <summary>Scores a draft against the task it is meant to answer (SPEC.md §4.2).</summary>
    ValueTask<Judgement> EvaluateAsync(string task, string candidate);
}
