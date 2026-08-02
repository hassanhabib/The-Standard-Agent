// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Foundations.Brains;

public static class RedactionRules
{
    public static IReadOnlyList<RedactionRule> Default { get; } =
    [
        new RedactionRule
        {
            Label = "EMAIL",
            Pattern = @"[\w.+-]+@[\w-]+\.[\w.-]+"
        },

        new RedactionRule
        {
            Label = "SSN",
            Pattern = @"\b\d{3}-\d{2}-\d{4}\b"
        },

        new RedactionRule
        {
            Label = "CREDIT_CARD",
            Pattern = @"\b\d{4}[ -]?\d{4}[ -]?\d{4}[ -]?\d{4}\b"
        },

        new RedactionRule
        {
            Label = "PHONE",
            Pattern = @"\b\d{3}[-.]\d{3}[-.]\d{4}\b"
        },
    ];
}
