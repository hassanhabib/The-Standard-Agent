// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.Services.Foundations.Data;

public interface ISkillService
{
    ValueTask<string> RetrieveSkillsAsync();
}
