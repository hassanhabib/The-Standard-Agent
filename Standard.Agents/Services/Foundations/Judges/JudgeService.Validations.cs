// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Judges.Exceptions;

namespace Standard.Agents.Services.Foundations.Judges;

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
