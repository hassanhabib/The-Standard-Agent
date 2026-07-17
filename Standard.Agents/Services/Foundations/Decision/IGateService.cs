// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Services.Foundations.Decision;

public interface IGateService
{
    ValueTask<string> ScreenAsync(string gatePrompt, string input);
}
