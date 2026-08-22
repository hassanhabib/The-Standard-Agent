// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Contracts;

// The Custom mode's substrate: host-supplied validation, for rules a schema cannot express —
// a total that must equal the sum of its lines, an account that must exist.
public sealed class FunctionContractBroker : IContractBroker
{
    private readonly Func<string, string, ValueTask<string?>> validate;

    public FunctionContractBroker(Func<string, string, ValueTask<string?>> validate) =>
        this.validate = validate;

    public ValueTask<string?> ValidateAsync(string answer, string schema) =>
        this.validate(answer, schema);
}
