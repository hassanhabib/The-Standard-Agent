// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Models.Foundations.Judges;

public record Judgement
{
    public double Score { get; init; }
    public string Reason { get; init; } = string.Empty;
}
