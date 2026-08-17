// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.RegularExpressions;
using Standard.Agents.Models.Foundations.Brains;

namespace Standard.Agents.Brokers.Redactions;

// Redaction at the model boundary (SPEC.md §4.6), extracted from the Brain so every model the
// agent drives can share it. The Gate screens the raw task and the Judge reads the task and the
// draft, so both see exactly the values redaction exists to hide — and a guardian may run on a
// different host than the Brain, which makes an unredacted guardian a wider exposure, not a
// narrower one.
//
// The rules are Data (RedactionRules.Default); this broker is the liaison to the regex engine
// that applies them, and holds no policy of its own.
public sealed class RuleRedactionBroker : IRedactionBroker
{
    private readonly IReadOnlyList<RedactionRule> rules;

    public RuleRedactionBroker(IEnumerable<RedactionRule> rules) =>
        this.rules = [.. rules];

    public string Redact(string text, IDictionary<string, string> vault)
    {
        if (this.rules.Count == 0 || string.IsNullOrEmpty(text))
        {
            return text;
        }

        string redactedText = text;

        foreach (RedactionRule rule in this.rules)
        {
            redactedText = Regex.Replace(
                redactedText,
                rule.Pattern,
                match => Tokenize(rule, match.Value, vault));
        }

        return redactedText;
    }

    public string Rehydrate(string text, IReadOnlyDictionary<string, string> vault)
    {
        string rehydratedText = text;

        foreach (KeyValuePair<string, string> entry in vault)
        {
            rehydratedText = rehydratedText.Replace(entry.Key, entry.Value);
        }

        return rehydratedText;
    }

    // One token per distinct value, so the same email redacts to the same placeholder twice and
    // the model can still reason about it being the same person.
    private static string Tokenize(
        RedactionRule rule,
        string value,
        IDictionary<string, string> vault)
    {
        string existingToken =
            vault.FirstOrDefault(entry => entry.Value == value).Key;

        if (existingToken is not null)
        {
            return existingToken;
        }

        string token = "{{" + rule.Label + "_" + vault.Count + "}}";
        vault[token] = value;

        return token;
    }
}
