// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Judges.Exceptions;

namespace Standard.Agents.Services.Foundations.Decision;

public partial class JudgeService
{
    private const double MinimumScore = 0.0;
    private const double MaximumScore = 1.0;

    private static void ValidateEvaluate(string judgePrompt, string candidate)
    {
        if (string.IsNullOrWhiteSpace(judgePrompt) || string.IsNullOrWhiteSpace(candidate))
        {
            throw new InvalidJudgeException(
                message: "Invalid judge input. Please correct the error and try again.");
        }
    }

    // Outgoing validation — the only one here. SPEC.md 4.1 makes 0.0..1.0 normative,
    // and the broker cannot enforce it (no flow control), so it lands here or nowhere.
    //
    // Written as "not within range" rather than "outside range" on purpose: NaN fails
    // every comparison, so `score < 0.0 || score > 1.0` would let it through. NaN then
    // also fails ThinkAsync's `score < 0.3` gate, making a garbage score
    // unrejectable rather than merely wrong.
    private static void ValidateScore(double score)
    {
        bool isWithinRange = score >= MinimumScore && score <= MaximumScore;

        if (isWithinRange is false)
        {
            throw new InvalidJudgeScoreException(
                message: "Invalid judge score. Score must be between 0.0 and 1.0.");
        }
    }
}
