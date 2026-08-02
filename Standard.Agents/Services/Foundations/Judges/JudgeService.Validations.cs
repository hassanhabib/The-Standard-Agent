// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Globalization;
using Standard.Agents.Models.Foundations.Judges;
using Standard.Agents.Models.Foundations.Judges.Exceptions;

namespace Standard.Agents.Services.Foundations.Judges;

public partial class JudgeService
{
    private const double MinimumScore = 0.0;
    private const double MaximumScore = 1.0;
    private const string ScoreLabel = "SCORE:";
    private const string ReasonLabel = "REASON:";

    private static void ValidateEvaluate(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            throw new InvalidJudgeException(
                message: "Invalid judge input. Please correct the error and try again.");
        }
    }

    private static Judgement ParseJudgement(string verdict)
    {
        string text = verdict?.Trim() ?? string.Empty;

        string? labeledScore = ExtractLabeled(text, ScoreLabel);

        string scoreText = labeledScore is null
            ? text
            : labeledScore.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? labeledScore;

        bool isNumeric = double.TryParse(
            scoreText,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double score);

        if (isNumeric is false)
        {
            throw new InvalidJudgeScoreException(
                message: "Invalid judge score. Score must be a number between 0.0 and 1.0.");
        }

        return new Judgement { Score = score };
    }

    private static string? ExtractLabeled(string text, string label)
    {
        foreach (string line in text.Split('\n'))
        {
            string trimmedLine = line.Trim();

            if (trimmedLine.StartsWith(label, StringComparison.OrdinalIgnoreCase))
            {
                return trimmedLine[label.Length..].Trim();
            }
        }

        return null;
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
