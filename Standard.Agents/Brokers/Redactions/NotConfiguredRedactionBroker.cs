// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Redactions;

// The default. SPEC.md §4.6: redaction is opt-in, and absent it the agent behaves exactly as
// if the section did not exist — no scanning, no vault, no cost.
public sealed class NotConfiguredRedactionBroker : IRedactionBroker
{
    public string Redact(string text, IDictionary<string, string> vault) =>
        text;

    public string Rehydrate(string text, IReadOnlyDictionary<string, string> vault) =>
        text;
}
