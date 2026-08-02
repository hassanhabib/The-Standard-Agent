// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text;
using System.Text.RegularExpressions;
using Standard.Agents.Models.Foundations.Brains;

namespace Standard.Agents.Services.Foundations.Brains;

public partial class BrainService
{
    private static string DrainReady(
        StringBuilder pending,
        IReadOnlyDictionary<string, string> vault)
    {
        string buffer = Rehydrate(pending.ToString(), vault);

        int hold = buffer.LastIndexOf('{');

        while (hold > 0 && buffer[hold - 1] == '{')
        {
            hold--;
        }

        string ready = hold >= 0 ? buffer[..hold] : buffer;
        string keep = hold >= 0 ? buffer[hold..] : string.Empty;

        pending.Clear();
        pending.Append(keep);

        return ready;
    }

    private string Redact(string text, IDictionary<string, string> vault)
    {
        if (this.redactionRules.Count == 0 || string.IsNullOrEmpty(text))
        {
            return text;
        }

        string redactedText = text;

        foreach (RedactionRule rule in this.redactionRules)
        {
            redactedText = Regex.Replace(
                redactedText,
                rule.Pattern,
                match => Tokenize(rule, match.Value, vault));
        }

        return redactedText;
    }

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

    private static string Rehydrate(string text, IReadOnlyDictionary<string, string> vault)
    {
        string rehydratedText = text;

        foreach (KeyValuePair<string, string> entry in vault)
        {
            rehydratedText = rehydratedText.Replace(entry.Key, entry.Value);
        }

        return rehydratedText;
    }
}
