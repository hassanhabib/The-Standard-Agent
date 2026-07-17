// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Services.Foundations.Judges;

public interface IJudgeService
{
    ValueTask<double> EvaluateAsync(string candidate);
}
