// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Verifiers;

public sealed class RuleVerifierBroker : IVerifierBroker
{
    private const string Pass = "SCORE: 1.0\nREASON: ok";

    private readonly IReadOnlyList<string> requiredPatterns;

    public RuleVerifierBroker(IEnumerable<string> requiredPatterns) =>
        this.requiredPatterns = requiredPatterns.ToList();

    public async ValueTask<string> VerifyAsync(string task, string candidate)
    {
        await Task.CompletedTask;

        string? missingPattern = this.requiredPatterns.FirstOrDefault(pattern =>
            candidate.Contains(pattern, StringComparison.OrdinalIgnoreCase) is false);

        return missingPattern is null
            ? Pass
            : $"SCORE: 0.0\nREASON: missing required content '{missingPattern}'";
    }
}
