// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Globalization;
using Standard.Agents.Models.Foundations.Judges.Exceptions;

namespace Standard.Agents.Services.Foundations.Judges;

public partial class JudgeService
{
    private const double MinimumScore = 0.0;
    private const double MaximumScore = 1.0;

    private static void ValidateEvaluate(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            throw new InvalidJudgeException(
                message: "Invalid judge input. Please correct the error and try again.");
        }
    }

    private static double ParseScore(string verdict)
    {
        bool isNumeric = double.TryParse(
            verdict?.Trim(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double score);

        if (isNumeric is false)
        {
            throw new InvalidJudgeScoreException(
                message: "Invalid judge score. Score must be a number between 0.0 and 1.0.");
        }

        return score;
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
