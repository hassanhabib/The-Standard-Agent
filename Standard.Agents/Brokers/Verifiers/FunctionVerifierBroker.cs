// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Verifiers;

public sealed class FunctionVerifierBroker : IVerifierBroker
{
    private readonly Func<string, string, ValueTask<string>> evaluate;
    private readonly string systemPrompt;

    public FunctionVerifierBroker(
        Func<string, string, ValueTask<string>> evaluate,
        string systemPrompt)
    {
        this.evaluate = evaluate;
        this.systemPrompt = systemPrompt;
    }

    // A host-supplied judge is handed the task and the candidate together, so the delegate's
    // second argument stays a single piece of text while still carrying what SPEC.md §4.2
    // requires the Judge to see. The labels are the same ones the judge contract already uses.
    public async ValueTask<string> VerifyAsync(string task, string candidate) =>
        await this.evaluate(this.systemPrompt, FormatVerdictRequest(task, candidate));

    internal static string FormatVerdictRequest(string task, string candidate) =>
        string.IsNullOrWhiteSpace(task)
            ? candidate
            : $"TASK: {task}{Environment.NewLine}{Environment.NewLine}ANSWER: {candidate}";
}
