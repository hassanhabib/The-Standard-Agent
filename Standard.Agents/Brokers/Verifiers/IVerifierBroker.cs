// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Verifiers;

public interface IVerifierBroker
{
    // The task travels with the candidate: an answer is good or bad FOR a question, and a
    // verdict on a fit the verifier cannot see is noise dressed as a number (SPEC.md §4.2).
    ValueTask<string> VerifyAsync(string task, string candidate);
}
