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

        return Accept;
    }

    public async ValueTask<string> AssessAsync(string systemPrompt, string input)
    {
        await Task.CompletedTask;

        return NoConflict;
    }
}
