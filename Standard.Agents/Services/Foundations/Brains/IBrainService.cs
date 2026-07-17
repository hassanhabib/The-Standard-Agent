// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Services.Foundations.Brains;

public interface IBrainService
{
    ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt);
}
