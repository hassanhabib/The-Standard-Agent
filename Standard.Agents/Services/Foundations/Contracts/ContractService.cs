// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Contracts;
using Standard.Agents.Brokers.Loggings;
using Standard.Agents.Models.Foundations.Contracts;

namespace Standard.Agents.Services.Foundations.Contracts;

// Whether an answer is the right SHAPE — the question the client contract could never answer.
//
// ProcessPromptAsync returns a string, so every consumer wiring an agent into a workflow wrote a
// parser, which is the thing this framework talked them out of doing everywhere else. Native tool
// calling gives structured input TO tools; the agent's own answer was always prose.
//
// It sits beside the Judge rather than anywhere else because it is the same kind of verdict at the
// same moment: the Judge asks whether a draft is good enough, this asks whether it is the right
// shape, and both run before a draft becomes an answer.
public partial class ContractService : IContractService
{
    private readonly IContractBroker contractBroker;
    private readonly ILoggingBroker loggingBroker;

    public ContractService(
        IContractBroker contractBroker,
        ILoggingBroker loggingBroker)
    {
        this.contractBroker = contractBroker;
        this.loggingBroker = loggingBroker;
    }

    public ValueTask<ContractVerdict> CheckAsync(string answer, string schema) =>
    TryCatch(async () =>
    {
        ValidateAnswer(answer);

        // No schema is not a failed check, it is no check. An agent given no contract is not
        // constrained by having become checkable — the same rule as counting always being on
        // while blocking is not.
        if (string.IsNullOrWhiteSpace(schema))
        {
            return ContractVerdict.Unconstrained;
        }

        string? complaint = await this.contractBroker.ValidateAsync(answer, schema);

        return string.IsNullOrWhiteSpace(complaint)
            ? ContractVerdict.Unconstrained
            : new ContractVerdict(Satisfied: false, Reason: complaint);
    });
}
