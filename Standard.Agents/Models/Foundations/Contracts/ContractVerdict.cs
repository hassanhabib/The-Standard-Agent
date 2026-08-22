// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Foundations.Contracts;

/// <summary>
/// Whether an answer is the right <i>shape</i>, beside the Judge's verdict on whether it is good
/// enough. Both are verdicts on a draft at the same moment, which is why this is a guardian and
/// not a new concept.
/// </summary>
/// <param name="Satisfied">Whether the answer matches the schema it was held to.</param>
/// <param name="Reason">
/// What is wrong with it, in words a model can act on. This is fed back as the reason a draft was
/// rejected, so "expected an object" is worth more than "invalid" — a revision the model cannot
/// aim at is a turn spent for nothing.
/// </param>
public record ContractVerdict(bool Satisfied, string Reason)
{
    /// <summary>The verdict when no contract was configured: everything satisfies nothing.</summary>
    public static ContractVerdict Unconstrained { get; } =
        new ContractVerdict(Satisfied: true, Reason: string.Empty);
}
