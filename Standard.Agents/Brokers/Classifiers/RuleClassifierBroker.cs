// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Classifiers;

public sealed class RuleClassifierBroker : IClassifierBroker
{
    private const string Accept = "accept";
    private const string NoConflict = "NONE";

    private readonly IReadOnlyList<string> refusePatterns;

    public RuleClassifierBroker(IEnumerable<string> refusePatterns) =>
        this.refusePatterns = refusePatterns.ToList();

    public async ValueTask<string> ClassifyAsync(string input)
    {
        await Task.CompletedTask;

        string? matchedRule = this.refusePatterns.FirstOrDefault(pattern =>
            input.Contains(pattern, StringComparison.OrdinalIgnoreCase));

        return matchedRule is null
            ? Accept
            : $"refuse: blocked by rule '{matchedRule}'";
    }

    public async ValueTask<string> AssessAsync(string systemPrompt, string input)
    {
        await Task.CompletedTask;

        return NoConflict;
    }
}
