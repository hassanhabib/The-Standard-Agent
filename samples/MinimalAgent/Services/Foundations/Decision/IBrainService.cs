// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace MinimalAgent.Services.Foundations.Decision;

public interface IBrainService
{
    ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt);
}
