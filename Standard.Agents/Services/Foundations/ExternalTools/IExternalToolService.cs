// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Services.Foundations.ExternalTools;

public interface IExternalToolService
{
    ValueTask<string> CallAsync(string name, string input);
}
