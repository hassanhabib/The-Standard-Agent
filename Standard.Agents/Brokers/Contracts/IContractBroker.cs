// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Contracts;

// The liaison to whatever knows how to validate a document against a schema: a validator in the
// box, a full JSON Schema library, or the host's own rules.
public interface IContractBroker
{
    /// <summary>
    /// Returns <c>null</c> when the answer satisfies the schema, and otherwise what is wrong with
    /// it — in words a model can act on, because the text becomes the reason a draft was rejected.
    /// </summary>
    ValueTask<string?> ValidateAsync(string answer, string schema);
}
