// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Foundations.Brains;

public record RedactionRule
{
    public string Label { get; init; } = string.Empty;
    public string Pattern { get; init; } = string.Empty;
}
