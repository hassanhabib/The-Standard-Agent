// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Brokers.Redactions;

// The Custom mode of redaction (SPEC.md §4.8): the host's own tokenize/restore pair, for rules a
// regex cannot express — an entity model, a domain dictionary, a data-residency policy. The
// decorators apply it at the wire exactly as they apply the built-in rules, so "every model call
// is redacted" stays true by construction whichever mode supplied the redactor.
public sealed class FunctionRedactionBroker : IRedactionBroker
{
    private readonly Func<string, IDictionary<string, string>, string> redact;
    private readonly Func<string, IReadOnlyDictionary<string, string>, string> rehydrate;

    public FunctionRedactionBroker(
        Func<string, IDictionary<string, string>, string> redact,
        Func<string, IReadOnlyDictionary<string, string>, string> rehydrate)
    {
        this.redact = redact;
        this.rehydrate = rehydrate;
    }

    public string Redact(string text, IDictionary<string, string> vault) =>
        this.redact(text, vault);

    public string Rehydrate(string text, IReadOnlyDictionary<string, string> vault) =>
        this.rehydrate(text, vault);
}
