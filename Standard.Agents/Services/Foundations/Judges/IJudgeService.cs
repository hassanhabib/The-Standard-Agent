// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Judges;

namespace Standard.Agents.Services.Foundations.Judges;

public interface IJudgeService
{
    ValueTask<Judgement> EvaluateAsync(string task, string candidate);
}
